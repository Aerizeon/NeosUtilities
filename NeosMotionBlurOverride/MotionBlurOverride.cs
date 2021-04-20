using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering.PostProcessing;

namespace NeosMotionBlurOverride
{
    [Category("Epsilion")]
    class MotionBlurOverride : Component, ICustomInspector
    {
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
                //Ignore cameras that aren't for screen mode.
                if (headOutputDevice.Type == HeadOutput.HeadOutputType.Screen)
                {
                    UniLog.Log($"[Motion Blur]\tFound Screen Mode HeadOuptut");
                    //Disable motion blur for future changes to this output
                    headOutputDevice.AllowMotionBlur = false;
                    //For each camera allocated to the HeadOutput, disable Motion Blur
                    //In screen mode, there should only be one camera.
                    foreach (UnityEngine.Camera cam in headOutputDevice.cameras)
                    {
                        if (cam.enabled)
                        {
                            PostProcessLayer postProc = cam.GetComponent<PostProcessLayer>();
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
