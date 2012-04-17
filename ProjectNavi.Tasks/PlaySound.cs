using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace ProjectNavi.Tasks
{
    public class PlaySound : TaskProvider
    {
        public string SoundName { get; set; }

        public override IEnumerable<Action<GameTime>> Action(Game game, IServiceProvider provider)
        {
            var sound = game.Content.Load<SoundEffect>(SoundName);
            using (var instance = sound.CreateInstance())
            {
                instance.Play();
                while (instance.State == SoundState.Playing)
                {
                    yield return gameTime => { };
                }
            }
        }
    }
}
