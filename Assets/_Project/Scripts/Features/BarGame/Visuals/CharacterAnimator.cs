using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Startup.Features.BarGame
{
    // Plays clips embedded in an imported character model by name, without an Animator Controller asset.
    public class CharacterAnimator : MonoBehaviour
    {
        private const string PreviewClipPrefix = "__preview__";
        private const string IdleClip = "idle";
        private const string WalkClip = "walk";
        private const string SitClip = "sit";
        private const string EatClip = "interact-right";

        private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();

        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _current;
        private AnimationClip _playing;
        private bool _loop;

        public void Initialize(Animator animator, string modelResourcePath)
        {
            foreach (var clip in Resources.LoadAll<AnimationClip>(modelResourcePath))
            {
                if (!clip.name.StartsWith(PreviewClipPrefix))
                {
                    _clips[clip.name] = clip;
                }
            }

            // GameTime mode follows Time.timeScale, so characters freeze while paused.
            _graph = PlayableGraph.Create("Character");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
            _graph.Play();
        }

        // Applies the current clip to the bones right now, so the model can be measured in its real pose.
        public void ApplyPoseNow()
        {
            if (_graph.IsValid())
            {
                _graph.Evaluate(0f);
            }
        }

        public void PlayIdle()
        {
            Play(IdleClip, true);
        }

        public void PlayWalk()
        {
            Play(WalkClip, true);
        }

        // Sitting holds its final pose instead of looping.
        public void PlaySit()
        {
            Play(SitClip, false);
        }

        public void PlayEat()
        {
            Play(EatClip, true);
        }

        private void Play(string clipName, bool loop)
        {
            if (!_graph.IsValid() || !_clips.TryGetValue(clipName, out var clip) || clip == _playing)
            {
                return;
            }

            if (_current.IsValid())
            {
                _current.Destroy();
            }

            _current = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(_current);
            _playing = clip;
            _loop = loop;
        }

        private void Update()
        {
            if (!_current.IsValid() || _playing.length <= 0f || _current.GetTime() < _playing.length)
            {
                return;
            }

            // Imported clips do not loop by themselves: restart, or hold the last frame.
            if (_loop)
            {
                _current.SetTime(0d);
            }
            else
            {
                _current.SetTime(_playing.length);
                _current.Pause();
            }
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}
