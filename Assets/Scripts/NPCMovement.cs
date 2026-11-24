using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform missionTarget;
    [SerializeField] private float stopDistance = 2.5f;

    [SerializeField] private Image cameraVignette;
    [SerializeField] private Color alertColor = new Color(1f, 0.1f, 0.1f, 0.4f);

    private NavMeshAgent agent;
    private NPCDialogue dialogue;
    private string lastValidIntention = "neutro";

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dialogue = GetComponent<NPCDialogue>();
    }

    private void Update()
    {
        string intention = dialogue != null ? dialogue.CurrentIntention : lastValidIntention;

        switch (intention)
        {
            case "ameaca":
                agent.isStopped = true;
                SetVignette(true);
                break;

            case "missao":
                SetVignette(false);
                agent.isStopped = false;
                if (missionTarget != null)
                    agent.SetDestination(missionTarget.position);
                break;

            default:
                SetVignette(false);
                agent.isStopped = false;
                if (player != null)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist > stopDistance)
                        agent.SetDestination(player.position);
                    else
                        agent.ResetPath();
                }
                break;
        }

        lastValidIntention = intention;
    }

    private void SetVignette(bool on)
    {
        if (cameraVignette == null) return;
        cameraVignette.color = on ? alertColor : Color.clear;
    }
}
