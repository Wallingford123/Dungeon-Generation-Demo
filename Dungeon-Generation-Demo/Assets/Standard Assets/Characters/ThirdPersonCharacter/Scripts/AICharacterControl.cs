using System;
using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class AICharacterControl : MonoBehaviour
    {
        public UnityEngine.AI.NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
        public float maxStoppingDistance;
        public LayerMask mask;
        public float sphereRadius;

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();

	        agent.updateRotation = false;
	        agent.updatePosition = true;
        }


        private void Update()
        {
            if (agent.isOnNavMesh && agent.isActiveAndEnabled)
            {
                if (target != null)
                    agent.SetDestination(target.position);


                if (agent.remainingDistance > agent.stoppingDistance)
                {
                    //Debug.DrawRay(transform.position, Vector3.forward * 100, Color.red);
                    if (agent.remainingDistance < maxStoppingDistance)
                    {
                        Collider[] colls = Physics.OverlapSphere(transform.position + transform.forward / 4 + new Vector3(0, .5f, 0), sphereRadius, mask);
                        if (!(colls.Length > 0))
                        {
                            character.Move(agent.desiredVelocity, false, false);
                        }
                        else
                        {
                            if (colls[0].gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().desiredVelocity == Vector3.zero)
                            {
                                character.Move(Vector3.zero, false, false);
                                agent.isStopped = true;
                            }
                            else character.Move(agent.desiredVelocity, false, false);
                        }
                    }
                    else
                    {
                        if (agent.isStopped)
                            agent.isStopped = false;
                        character.Move(agent.desiredVelocity, false, false);
                    }
                }
                else
                    character.Move(Vector3.zero, false, false);
            }
        }


        public void SetTarget(Transform target)
        {
            this.target = target;
        }

    }
}
