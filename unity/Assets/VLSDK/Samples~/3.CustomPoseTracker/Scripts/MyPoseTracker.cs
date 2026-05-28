using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARCeye;

namespace ARCeye.Example
{
    public class MyPoseTracker : PoseTracker
    {
        private int index = 0;
        private int maxIndex = 5;

        /// Event executed when MyPoseTracker is created.
        public override void OnCreate(Config config)
        {
            // Disable all filtering for testing purposes.
            config.tracker.useTranslationFilter = false;
            config.tracker.useRotationFilter = false;
            config.tracker.useInterpolation = false;
            config.tracker.useLocalVLSearch = false;
        }

        /// The rate at which CreateARFrame is called.
        /// In this example, it is set to execute 1 frame per second.
        protected override float GetTargetFrameRate() => 1f;

        /// Method called every frame to create an ARFrame.
        /// Converts camera preview and camera pose information provided by the device into ARFrame format and returns it.
        protected override ARFrame CreateARFrame()
        {
            index = index % maxIndex + 1;

            var indexStr = index.ToString("D2");
            var texture = Resources.Load<Texture2D>($"Dataset/{indexStr}");

            ARFrame frame = new ARFrame();

            frame.texture = texture;
            frame.localPosition = new Vector3(index, 0, 0);

            return frame;
        }
    }

}