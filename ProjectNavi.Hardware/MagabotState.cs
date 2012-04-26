using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectNavi.Hardware;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ProjectNavi.Hardware
{
    public class MagabotState :HardwareComponent
    {
        private BumperBoard bumpers;
        private BatteryBoard battery;
        private GroundSensorBoard groundSensors;
        private SonarsBoard sonars;
        private IObservable<bool> safetyBump;
        private IObservable<bool> safetyGround;
        private Subject<string> markerActivated;

        public bool Stopped { get; private set; }

        public enum NavigationMode
        {
            Direct = 0,
            Assisted = 1,
            ObstacleAvoidance = 2,
            
        }
        
        public MagabotState(LedBoard leds, DifferentialSteeringBoard steering, BumperBoard bumpers, BatteryBoard battery, GroundSensorBoard groundSensors, SonarsBoard sonars)
        {
            Leds = leds;
            Stopped = true;
            DifferentialSteering = steering;
            Sonar = new double[5];
            this.bumpers = bumpers;
            this.battery = battery;
            this.groundSensors = groundSensors;
            this.sonars = sonars;
            this.battery.BatteryMeasure.Subscribe( m => Battery = m);
            this.sonars.SonarsBoardMeasure.Subscribe(SonarsMeasure);
            IRGroundThreshold = 1200;
            bumpers.BumpersMeasure.Subscribe( m => BumperSensorState = m);

            safetyBump = (from measurement in bumpers.BumpersMeasure.Do(measurement => BumperSensorState = measurement)
                          select measurement.BumperLeft || measurement.BumperRight)
                   .DistinctUntilChanged()
                   .Select(bumpState =>
                   {
                       if (bumpState)
                       {
                           if (Navigation != NavigationMode.Direct)
                               Backward();
                       }
                       else if (Navigation != NavigationMode.Direct)
                       {
                           return (from tick in Observable.Timer(TimeSpan.FromMilliseconds(SafetyTimeMilis))
                                  select bumpState)
                                  .Do( m=> Stop());
                       }
                       return Observable.Repeat(bumpState, 1);
                   })
                   .Switch()
                   .Publish()
                   .RefCount();
            safetyGround = (from measurement in groundSensors.GroundSensorsMeasure.Do(measurement => GroundSensorState = measurement)
                        select measurement.SensorLeft > IRGroundThreshold ||
                               measurement.SensorMiddle > IRGroundThreshold ||
                               measurement.SensorRight > IRGroundThreshold)
                    .DistinctUntilChanged()
                    .Select(groundState =>
                    {
                        if (groundState)
                        {
                            if (Navigation != NavigationMode.Direct)
                                Backward();
                        }
                        else if (Navigation != NavigationMode.Direct)
                        {
                            return (from tick in Observable.Timer(TimeSpan.FromMilliseconds(SafetyTimeMilis))
                                   select groundState)
                                   .Do(m => Stop());
                        }
                        return Observable.Repeat(groundState, 1);
                    })
                    .Switch()
                    .Publish()
                    .RefCount();
            safetyGround.Subscribe();
            safetyBump.Subscribe();
            markerActivated = new Subject<string>();
            MaxVelocity = 10;
            MaxTurnVelocity = 5;
            SafetyTimeMilis = 1000;
        }

        public double MaxVelocity { get; set; }
        public double MaxTurnVelocity { get; set; }
        public double Battery { get; private set; }
        public double[] Sonar { get; private set; }
        public BumpersMeasurement BumperSensorState  { get; private set; }

        public GroundSensorsMeasurement GroundSensorState { get; private set; }
        
        public int IRGroundThreshold { get; set; }
        public NavigationMode Navigation { get; set; }
        public int SafetyTimeMilis { get; set; }

        public LedBoard Leds { get; private set; }
        public DifferentialSteeringBoard DifferentialSteering { get; private set; }

        public IObservable<bool> SafetyBump 
        {
            get
            {
                return safetyBump;
            }
        }
        public IObservable<bool> SafetyGround
        {
            get 
            {
                return safetyGround;
            }
        }

        public IObservable<string> MarkerActivated
        {
            get
            {
                return markerActivated;
            }
        }

        private void SonarsMeasure(short[] measures)
        {
            for (int count = 0; count < Sonar.Length; count++)
                Sonar[count] = measures[count];
        }

//                    .DistinctUntilChanged()
//(GroundSensorsMeasurement measure)
//        {

//            iRGround[0] = measure.SensorLeft;
//            iRGround[1] = measure.SensorMiddle;
//            iRGround[2] = measure.SensorRight;
//            var newGroundState = iRGround.Count(m => m > IRGroundThreshold)>0;
//            if (groundState != newGroundState)
//            {
//                groundState = newGroundState;
//                if (newGroundState)
//                    safetyGround.OnNext(newGroundState);
//                else if (!(Navigation == NavigationMode.Direct))
//                {
//                    Observable.Timer(TimeSpan.FromSeconds(SafetyTimeMilis)).Subscribe(m => safetyGround.OnNext(newGroundState));
//                    Backward();
//                }
//                else
//                    safetyGround.OnNext(newGroundState);
                
//            }
//        }

        //private void BumperMeasure(BumpersMeasurement measure)
        //{
        //    if (BumperLeft != measure.BumperLeft)
        //    {
        //        BumperLeft = measure.BumperLeft;
        //        safetyBump.OnNext(new BumpEvent(Bumper.BumperLeft, measure.BumperLeft));
        //    }
        //    if (BumperRight != measure.BumperRight)
        //    {
        //        // New RightEvent
        //        BumperRight = measure.BumperRight;
        //        safetyBump.OnNext(new BumpEvent(Bumper.BumperRight, measure.BumperRight));
        //    }
        //}

        public void ActivateMarker(string name)
        {
            markerActivated.OnNext(name);
        }

        public void Forward()
        {
            this.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(MaxVelocity, MaxVelocity));
            Stopped = false;
        }
        public void Backward()
        {
            Stopped = false;
            DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(-MaxVelocity, -MaxVelocity));  
        }
        public void Left()
        {
            Stopped = false;
            DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(MaxTurnVelocity, -MaxTurnVelocity));  
        }
        public void Right()
        {
            DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(-MaxTurnVelocity, MaxTurnVelocity));
            Stopped = false;
        }
        public void Stop()
        {
            DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0, 0));
            Stopped = true;
        }
        public void SimpleObstacleAvoidance()
        {
            throw new NotImplementedException();
        }

    }
    //public enum Bumper
    //{
    //    BumperLeft,
    //    BumperRight
    //}
    //public struct BumpEvent 
    //{
    //    Bumper bumper;
    //    bool state;

    //    public BumpEvent(Bumper bumper, bool state)
    //    {
    //        this.bumper = bumper;
    //        this.state = state;
    //    }

    //    public Bumper Bumper
    //    {
    //        get { return bumper; }
    //    }

    //    public bool State
    //    {
    //        get { return state; }
    //    }
    //}
}
