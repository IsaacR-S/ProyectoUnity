using UnityEngine;

/// <summary>
/// Maneja el movimiento del Príncipe Vela:
/// correr, saltar (doble salto), agacharse y dash.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ── Parámetros de movimiento ──────────────────────────────────────────────
    [Header("Movimiento")]
    [SerializeField] float moveSpeed      = 7f;
    [SerializeField] float jumpForce      = 16f;
    [SerializeField] float crouchSpeedMul = 0.5f;   // multiplicador al agacharse

    [Header("Doble Salto")]
    [SerializeField] int maxJumps = 2;               // 1 = solo salto simple, 2 = doble salto

    [Header("Dash")]
    [SerializeField] float dashSpeed      = 22f;
    [SerializeField] float dashDuration   = 0.18f;
    [SerializeField] float dashCooldown   = 0.8f;

    [Header("Detección de suelo")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float     groundCheckRadius = 0.15f;
    [SerializeField] LayerMask groundLayer;

    // ── Componentes ───────────────────────────────────────────────────────────
    Rigidbody2D rb;
    Animator    anim;
    WaxSystem   waxSystem;       // para bloquear acciones al morir

    // ── Estado interno ────────────────────────────────────────────────────────
    float  horizontalInput;
    bool   isGrounded;
    bool   isCrouching;
    bool   isDashing;
    int    jumpsLeft;
    float  dashTimer;
    float  dashCooldownTimer;
    bool   facingRight = true;

    // ── Propiedades públicas (leídas por PlayerCombat) ────────────────────────
    public bool  IsDashing    => isDashing;
    public bool  FacingRight  => facingRight;
    public bool  IsGrounded   => isGrounded;
    public float MoveSpeed    => moveSpeed;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        anim      = GetComponent<Animator>();
        waxSystem = GetComponent<WaxSystem>();
    }

    void Update()
    {
        if (waxSystem != null && waxSystem.IsDead) return;

        CheckGround();
        GatherInput();
        HandleJump();
        HandleDash();
        HandleCrouch();
        HandleFlip();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (waxSystem != null && waxSystem.IsDead) return;
        if (!isDashing)
            Move();
    }

    // ── Suelo ─────────────────────────────────────────────────────────────────
    void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && !wasGrounded)      // aterrizó
            jumpsLeft = maxJumps;
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        isCrouching     = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    // ── Movimiento horizontal ─────────────────────────────────────────────────
    void Move()
    {
        float speed = isCrouching ? moveSpeed * crouchSpeedMul : moveSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);
    }

    // ── Salto / doble salto ───────────────────────────────────────────────────
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (jumpsLeft > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsLeft--;
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxJump);
                anim.SetTrigger("Jump");
            }
        }
    }

    // ── Dash ──────────────────────────────────────────────────────────────────
    void HandleDash()
    {
        // cuenta atrás del dash activo
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxDash);
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.gravityScale = 1f;
            }
            return;
        }

        dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            isDashing             = true;
            dashTimer             = dashDuration;
            dashCooldownTimer     = dashCooldown;
            rb.gravityScale       = 0f;
            float dir             = facingRight ? 1f : -1f;
            rb.linearVelocity           = new Vector2(dir * dashSpeed, 0f);
            anim.SetTrigger("Dash");
        }
    }

    // ── Agacharse ─────────────────────────────────────────────────────────────
    void HandleCrouch()
    {
        anim.SetBool("Crouching", isCrouching);
    }

    // ── Voltear sprite ────────────────────────────────────────────────────────
    void HandleFlip()
    {
        if (horizontalInput > 0 && !facingRight)  Flip();
        if (horizontalInput < 0 &&  facingRight)  Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    // ── Animator ──────────────────────────────────────────────────────────────
    void UpdateAnimator()
    {
        anim.SetFloat("Speed",   Mathf.Abs(horizontalInput));
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VelY",    rb.linearVelocity.y);
    }

    // ── Debug visual ──────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
