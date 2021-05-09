using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using UnityEngine.Rendering.PostProcessing;

namespace NeosSimpleUtilities.MotionBlurOverride
{
    [Category("Add-Ons/Optimization")]
    class MotionBlurOverride : Component, ICustomInspector
    {
        public readonly Sync<bool> IgnoreVRCameras;
        protected override void OnAttach()
        {
            IgnoreVRCameras.Value = true;
            base.OnAttach();
        }
        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.PushStyle();
            ui.Style.MinHeight = 60f;
            ui.Text("This component is to be used for testing only.\nPlease do not include it in any public distributions.\nMessage Epsilion for more details.", true, Alignment.TopCenter);
            ui.PopStyle();
        }
        protected override void OnAwake()
        {
            EnabledField.OnValueChange += EnabledField_OnValueChange;
            OverrideMotionBlur();
            base.OnAwake();
        }

        private void EnabledField_OnValueChange(SyncField<bool> syncField)
        {
            OverrideMotionBlur();
        }

        public void OverrideMotionBlur()
        {
            HeadOutput[] headOutputDevices = UnityEngine.Object.FindObjectsOfType<HeadOutput>();
            UniLog.Log($"[Motion Blur]\tFound {headOutputDevices.Length} HeadOutput instances");
            foreach (HeadOutput headOutputDevice in headOutputDevices)
            {
                //Ignore cameras that aren't for screen mode. - unless we really want to suffer.
                if (headOutputDevice.Type == HeadOutput.HeadOutputType.Screen || !IgnoreVRCameras.Value)
                {
                    UniLog.Log($"[Motion Blur]\tFound Screen Mode HeadOuptut");
                    //Disable motion blur for future changes to this output
                    headOutputDevice.AllowMotionBlur = !Enabled;
                    //For each camera allocated to the HeadOutput, disable Motion Blur
                    //In screen mode, there should only be one camera.
                    foreach (UnityEngine.Camera outputCamera in headOutputDevice.cameras)
                    {
                        if (outputCamera.enabled)
                        {
                            PostProcessLayer postProc = outputCamera.GetComponent<PostProcessLayer>();
                            if (postProc is null)
                            {
                                UniLog.Log("[Motion Blur]\tOutput Camera has no PostProcessingLayer");
                                return;
                            }

                            MotionBlur motionBlurSetting = postProc.defaultProfile.GetSetting<MotionBlur>();
                            if (motionBlurSetting is null)
                            {
                                UniLog.Log("[Motion Blur]\tOutput Camera has no MotionBlur setting");
                                return;
                            }

                            motionBlurSetting.enabled.value = !Enabled;
                            UniLog.Log($"[Motion Blur]\tMotion Blur for HeadOutput { (Enabled ? "Disabled" : "Enabled")}");
                        }
                    }
                }
            }
        }
    }
}
