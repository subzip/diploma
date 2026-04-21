
using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerCamera;

    [Header("Settings")]
    [SerializeField] private float maxSpeed = 8f;

    private PlayerMovement playerMovement; 

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (playerMovement == null) return;

        float moveSpeed = playerMovement.GetMoveSpeed(); 
        float normalizedSpeed = Mathf.Clamp01(moveSpeed / maxSpeed);

        animator.SetFloat("Speed", normalizedSpeed);
    }
}