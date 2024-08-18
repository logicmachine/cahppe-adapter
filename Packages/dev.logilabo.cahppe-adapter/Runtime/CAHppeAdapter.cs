using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace dev.logilabo.cahppe_adapter.runtime
{

    [AddComponentMenu("Logilabo Avatar Tools/CAHppe Adapter")]
    [DisallowMultipleComponent]
    public class CAHppeAdapter : MonoBehaviour, IEditorOnly
    {
        [InspectorName("VirtualLens Settings")]
        public GameObject virtualLensSettings;
        [InspectorName("CAHppe Object")]
        public GameObject cahppeObject;
    }

}
