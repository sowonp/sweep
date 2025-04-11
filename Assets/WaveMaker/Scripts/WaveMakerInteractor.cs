using UnityEngine;

#if MATHEMATICS_INSTALLED
using Unity.Mathematics;
#endif

namespace WaveMaker
{
    [HelpURL("http://wavemaker.lidia-martinez.com/")]
    public class WaveMakerInteractor : MonoBehaviour
    {
        public event System.EventHandler OnDisabledOrDestroyed;

        [SerializeField]
        [Tooltip("Each interactor component is associated with just one collider in the same gameObject")]
        Collider associatedCollider;

        [Header("Only for surfaces using Velocity mode")]
        [Tooltip("Shows the linear and angular velocies in the scene view during play")]
        public bool showSpeed = false;

        [Tooltip("This will make velocity values change softly, making the response of the WaveMaker object softer too.")]
        public bool speedDampening = false;

        [Tooltip("Higher value means slower velocity change")]
        [Range(0, 1)]
        public float speedDampValue = 0f;

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        public bool Initialized { get; private set; }
        public NativeCollider NativeCollider { get; private set; }
        public float3 LinearVelocity { get; private set; }
        public float3 AngularVelocity { get; private set; }
        public Rigidbody RigidBody { get { return associatedCollider != null ? associatedCollider.attachedRigidbody : null; } }

        public Collider AssociatedCollider
        {
            get { return associatedCollider; }
            set
            {
                if(value is MeshCollider)
                    Utils.LogError("Mesh colliders are currently not supported in WaveMaker Interactors.", gameObject);
                else
                    associatedCollider = value;
            }
        }

        float4 _lastPosition;
        quaternion _lastRotation;

        private void Awake()
        {
            Initialized = false;
        }

        private void Reset()
        {
            if (associatedCollider == null)
                associatedCollider = GetFirstUnusedCollider();
        }

        void Update()
        {
            if (showSpeed)
            {
                UpdateVelocities();
                Debug.DrawRay(transform.position, LinearVelocity.xyz, Color.red);
                Debug.DrawRay(transform.position, AngularVelocity.xyz, Color.blue);
            }
        }

        public void Initialize()
        {
            Initialized = false;
            if (associatedCollider == null)
            {
                Utils.LogWarning("No collider attached to the Interactor component. Trying to find the first one or disabling the Interactor.", gameObject);
                associatedCollider = GetFirstUnusedCollider();
                if(associatedCollider == null)
                {
                    enabled = false;
                    return;
                }
            }

            if (RigidBody == null)
            {
                Utils.LogWarning("Without rigidbody in this or any parent, this interactor will not be detected by any surface. To manually make the surfaces detect it without RB, use AddInteractor and RemoveInteractor in the Surface API.", gameObject);
                return;
            }

            _lastPosition = new float4(transform.position, 0);
            _lastRotation = transform.rotation;
            Initialized = true;

            UpdateNativeCollider();
        }

        internal static WaveMakerInteractor GetRelatedInteractor(Collider collider)
        {
            if (!collider.TryGetComponent<WaveMakerInteractor>(out _))
                return null;

            foreach (var interactor in collider.GetComponents<WaveMakerInteractor>())
                if (interactor.AssociatedCollider == collider)
                    return interactor;

            return null;
        }

        internal Collider GetFirstUnusedCollider()
        {
            var others = GetComponents<WaveMakerInteractor>();
            var colliders = GetComponents<Collider>();

            foreach (var col in colliders)
            {
                bool found = false;
                if (col as MeshCollider != null)
                    continue;

                foreach (var interactor in others)
                {
                    if (interactor != this && interactor.AssociatedCollider == col)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return col;
            }

            return null;
        }

        public void UpdateNativeCollider()
        {
            if (!Initialized)
                return;

            NativeCollider = new NativeCollider(associatedCollider);
        }

        /// <summary>
        /// Only used when the Velocity Based interaction mode is enabled. In Occupancy mode, the RigidBody velocity is taken
        /// </summary>
        public void UpdateVelocities()
        {
            float3 oldLinearVelocity = LinearVelocity;

            if (RigidBody != null && !RigidBody.isKinematic && RigidBody.useGravity)
            {
                LinearVelocity = RigidBody.velocity;
                AngularVelocity = RigidBody.angularVelocity;
            }
            else
            {
                var curPos = new float4(transform.position, 0);
                LinearVelocity = ((curPos - _lastPosition) / Time.fixedDeltaTime).xyz;
                _lastPosition = curPos;

                AngularVelocity = Utils.GetAngularVelocity(_lastRotation, transform.rotation);
                _lastRotation = transform.rotation;
            }

            //TODO: Damping only works on velocity based mode
            if (speedDampening)
                LinearVelocity = math.lerp(oldLinearVelocity, LinearVelocity, 1 - speedDampValue);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            Initialize();
        }

        private void OnDisable()
        {
            Initialized = false;
            OnDisabledOrDestroyed?.Invoke(this, null);
        }

#endif
    }
}
