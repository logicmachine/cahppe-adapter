using System;
using dev.logilabo.cahppe_adapter.runtime;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VirtualLens2;
using Object = UnityEngine.Object;

namespace dev.logilabo.cahppe_adapter.editor
{
    public class CAHppeAdapterPass : Pass<CAHppeAdapterPass>
    {
        private enum CAHppeVersion { V1, V2 }

        private static CAHppeVersion DetectVersion(CAHppeAdapter config)
        {
            if (config?.cahppeObject == null) { throw new ArgumentException("CAHppe Object is not specified."); }
            var root = config.cahppeObject.transform;
            if (HierarchyUtility.PathToObject(root, "World Constraint 1/Camera") != null)
            {
                return CAHppeVersion.V1;
            }
            if (HierarchyUtility.PathToObject(root, "Camera/Smooth Look At") != null)
            {
                return CAHppeVersion.V2;
            }
            throw new ArgumentException("Failed to detect the CAHppe version.");
        }

        private static void ModifyVirtualLens2(CAHppeAdapter config, CAHppeVersion version)
        {
            var virtualLens = config.virtualLensSettings.GetComponent<VirtualLensSettings>();
            if (version == CAHppeVersion.V1)
            {
                virtualLens.externalPoseSource =
                    HierarchyUtility.PathToObject(config.cahppeObject.transform, "World Constraint 1/Camera");
            }
            else if(version == CAHppeVersion.V2)
            {
                virtualLens.externalPoseSource =
                    HierarchyUtility.PathToObject(config.cahppeObject.transform, "Camera/Smooth Look At");
            }
        }

        private static void ModifyCAHppe(CAHppeAdapter config, CAHppeVersion version)
        {
            // Remove camera and screen space renderer to override
            if (version == CAHppeVersion.V1)
            {
                var camera = HierarchyUtility.PathToObject(config.cahppeObject, "World Constraint 1/Camera");
                Object.DestroyImmediate(camera.GetComponent<Camera>());
                var screenSpace = HierarchyUtility.PathToObject(config.cahppeObject, "ScreenSpace");
                Object.DestroyImmediate(screenSpace.GetComponent<SkinnedMeshRenderer>());
            }
            else if (version == CAHppeVersion.V2)
            {
                var cameras = HierarchyUtility.PathToObject(config.cahppeObject, "Camera/Smooth Look At");
                foreach (var camera in cameras.GetComponentsInChildren<Camera>()) { Object.DestroyImmediate(camera); }
                var screenSpace = HierarchyUtility.PathToObject(config.cahppeObject, "ScreenSpace");
                Object.DestroyImmediate(screenSpace.GetComponent<SkinnedMeshRenderer>());
            }

            // Add animator controller to expose parameter `ScreenCam`
            var guid = "ae319e590870641469d9fac27db2177a";
            var controllerPath = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            foreach (var component in config.cahppeObject.GetComponents<Component>())
            {
                var so = new SerializedObject(component);
                if (so.targetObject.GetType().FullName != "VF.Model.VRCFury") { continue; }
                var content = so.FindProperty("content");
                if (content.propertyType != SerializedPropertyType.ManagedReference) { continue; }
                if (content.managedReferenceValue.GetType().FullName != "VF.Model.Feature.FullController") { continue; }
                {
                    // Add controller
                    var controllers = content.FindPropertyRelative("controllers");
                    var index = controllers.arraySize;
                    controllers.InsertArrayElementAtIndex(index);
                    var item = controllers.GetArrayElementAtIndex(index);
                    item.FindPropertyRelative("controller.version").intValue = 1;
                    item.FindPropertyRelative("controller.fileID").longValue = 0;
                    item.FindPropertyRelative("controller.id").stringValue = $"{guid}|{controllerPath}";
                    item.FindPropertyRelative("controller.objRef").objectReferenceValue = controller;
                    item.FindPropertyRelative("type").enumValueIndex = 5; // FX
                }
                {
                    // Add global params
                    var globalParams = content.FindPropertyRelative("globalParams");
                    var index = globalParams.arraySize;
                    globalParams.InsertArrayElementAtIndex(index);
                    var item = globalParams.GetArrayElementAtIndex(index);
                    item.stringValue = "CAHppeAdapter/ScreenCam";
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                break;
            }
        }

        private static void ExecuteSingle(BuildContext context, CAHppeAdapter config)
        {
            // TODO validate configurations
            var version = DetectVersion(config);
            ModifyVirtualLens2(config, version);
            ModifyCAHppe(config, version);
        }
        
        protected override void Execute(BuildContext context)
        {
            var components = context.AvatarRootObject.GetComponentsInChildren<CAHppeAdapter>();
            foreach (var config in components)
            {
                if (config.gameObject.tag == "EditorOnly") { continue; }
                ExecuteSingle(context, config);
                Object.DestroyImmediate(config);
            }
        }
    }
}
