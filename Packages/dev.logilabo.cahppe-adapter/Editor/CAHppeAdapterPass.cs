using System.Collections;
using System.Collections.Generic;

using dev.logilabo.cahppe_adapter.runtime;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VirtualLens2;

namespace dev.logilabo.cahppe_adapter.editor
{
    public class CAHppeAdapterPass : Pass<CAHppeAdapterPass>
    {
        private static void ModifyVirtualLens2(CAHppeAdapter config)
        {
            var virtualLens = config.virtualLensSettings.GetComponent<VirtualLensSettings>();
            var target = HierarchyUtility.PathToObject(config.cahppeObject.transform, "World Constraint 1/Camera");
            virtualLens.externalPoseSource = target;
        }

        private static void ModifyCAHppe(CAHppeAdapter config)
        {
            // Remove camera and screen space renderer to override
            var camera = HierarchyUtility.PathToObject(config.cahppeObject, "World Constraint 1/Camera");
            Object.DestroyImmediate(camera.GetComponent<Camera>());
            var screenSpace = HierarchyUtility.PathToObject(config.cahppeObject, "ScreenSpace");
            Object.DestroyImmediate(screenSpace.GetComponent<SkinnedMeshRenderer>());
            
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
            ModifyVirtualLens2(config);
            ModifyCAHppe(config);
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
