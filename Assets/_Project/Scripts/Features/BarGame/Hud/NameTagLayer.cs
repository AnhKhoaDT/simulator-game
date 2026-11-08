using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // Name labels that float above people's heads. Drawn by the HUD (so Vietnamese text renders like the rest
    // of the UI) and moved each frame to follow their target on screen.
    public sealed class NameTagLayer
    {
        private const float MaxDistance = 25f;
        private const float MinDepth = 0.2f;

        private readonly VisualElement _root;
        private readonly List<Tag> _tags = new List<Tag>();

        public NameTagLayer(VisualElement root)
        {
            _root = root;
        }

        // 'visible' lets a tag hide itself, e.g. the owner's own tag in first person.
        public Label Add(Transform target, float height, string text, Func<bool> visible = null)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("name-tag");
            label.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
            label.style.display = DisplayStyle.None;
            _root.Add(label);
            _tags.Add(new Tag(target, height, label, visible));
            return label;
        }

        public void Tick(Camera cam)
        {
            var panel = _root.panel;
            if (cam == null || panel == null)
            {
                return;
            }

            foreach (var tag in _tags)
            {
                var world = tag.Target.position + Vector3.up * tag.Height;
                var depth = Vector3.Dot(world - cam.transform.position, cam.transform.forward);
                var show = tag.Target.gameObject.activeInHierarchy
                           && (tag.Visible == null || tag.Visible())
                           && depth > MinDepth && depth < MaxDistance;

                tag.Label.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show)
                {
                    continue;
                }

                var position = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                tag.Label.style.left = position.x;
                tag.Label.style.top = position.y;
            }
        }

        private readonly struct Tag
        {
            public readonly Transform Target;
            public readonly float Height;
            public readonly Label Label;
            public readonly Func<bool> Visible;

            public Tag(Transform target, float height, Label label, Func<bool> visible)
            {
                Target = target;
                Height = height;
                Label = label;
                Visible = visible;
            }
        }
    }
}
