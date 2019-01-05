using UnityEngine;
using System.Collections;
using Prime31;

public enum bounceDirection
{
    right = 0,
    left,
    none
}

[System.Serializable]
class Dash
{
    public float dashSpeed = 30f;
    public float dashMaxDistance = 3.5f;
    public float upDashDamping = 15f;
    public float downDashDamping = 15f;
}

public class DemoScene : MonoBehaviour
{
	// movement config
    [Range(-100,0)]
	public float gravity = -80f;
	public float runSpeed = 8f;


    
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float minJumpHeight = 3f;
    [Range(1,5)]
    public float JumpDrag = 2.5f;


    KeyCode upKey = KeyCode.UpArrow;
    KeyCode downKey = KeyCode.DownArrow;
    KeyCode rightKey = KeyCode.RightArrow;
    KeyCode leftKey = KeyCode.LeftArrow;
    KeyCode jumpKey = KeyCode.C;
    KeyCode dashKey = KeyCode.X;
    KeyCode shotKey = KeyCode.Z;

    bool dashState = false;
    [SerializeField]
    int dashMaxTimes = 1;
    [SerializeField]
    Dash normalDash;
    [SerializeField]
    Dash superDash;

    Dash currentDash;

    int dashRestTime;
    float hasDashDistance;
    int delayFrame = 5;
    int CurrentFrame = 0;
    bool readyDash = false;

    //float upDashDamping, downDashDamping;

    bool bounceOnWallState = false;
    public float bounceForceX = 3, bounceForceY = 2;

    private float bounceDragX = 40;
    bounceDirection bounceDir;
    bool bounceDamping = false;
    public float bounceDampingSpeed = 10;

    bool pauseState = false;

	private float normalizedHorizontalSpeed = 0;
    Vector2 dashDirection;

	private CharacterController2D _controller;
	//private Animator _animator;
	//private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;


    [SerializeField]
    Transform rightArrow, leftArrow;

    [SerializeField]
    GameObject bulletPrefab;

    bool readyShot = false;

    Transform fetchedThings;

	void Awake()
	{
        //_animator = GetComponent<Animator>();
        currentDash = normalDash;
        _controller = GetComponent<CharacterController2D>();

		// listen to some events for illustration purposes
		_controller.onControllerCollidedEvent += onControllerCollider;
		_controller.onTriggerEnterEvent += onTriggerEnterEvent;
		_controller.onTriggerExitEvent += onTriggerExitEvent;
        _controller.OnWallBounceEvent += BeginBounceState;
    
	}


	#region Event Listeners

	void onControllerCollider( RaycastHit2D hit )
	{
		// bail out on plain old ground hits cause they arent very interesting
		if( hit.normal.y == 1f )
			return;

		// logs any collider hits if uncommented. it gets noisy so it is commented out for the demo
		//Debug.Log( "flags: " + _controller.collisionState + ", hit.normal: " + hit.normal );
	}


	void onTriggerEnterEvent( Collider2D col )
	{
		Debug.Log( "onTriggerEnterEvent: " + col.gameObject.name );
	}


	void onTriggerExitEvent( Collider2D col )
	{
		Debug.Log( "onTriggerExitEvent: " + col.gameObject.name );
	}

	#endregion


	// the Update loop contains a very simple example of moving the character around and controlling the animation
	void Update()
	{
        if(Input.GetKeyDown(KeyCode.Q))
        {
            if(fetchedThings == null)
            {
                fetchedThings = _controller.CarrayThings();
            }
            else
            {
                _controller.ThrowThings(fetchedThings);
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            BeginPauseState();
            SetArrowPos(rightArrow);
        }


        if(pauseState)
        {
            return;
        }

        if(dashState)
        {
            Dash();
            return;
        }
        else if(bounceOnWallState)
        {
            Bounce();
            return;
        }


        if (_controller.isGrounded)           
        {
            ResetDashTime();
            _velocity.y = 0;
        }
			

		if( Input.GetKey( rightKey) )
		{
            dashDirection = Vector2.right;
			normalizedHorizontalSpeed = 1;
			if( transform.localScale.x < 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
		}
		else if( Input.GetKey( leftKey ) )
		{
            dashDirection = Vector2.left;
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x > 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
		}
		else
		{
            dashDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
			normalizedHorizontalSpeed = 0;
		}
        //normalizedHorizontalSpeed = 1;

        if(Input.GetKey(upKey))
        {
            dashDirection = normalizedHorizontalSpeed == 0 ? new Vector2(0, 1)  : new Vector2(dashDirection.x, 1);
        }
        else if(Input.GetKey(downKey))
        {
            dashDirection = normalizedHorizontalSpeed == 0 ? new Vector2(0, -1) : new Vector2(dashDirection.x, -1);
        }

        if(Input.GetKeyDown(dashKey) && dashRestTime != 0)
        {
            //Dash();
            //return;
            readyDash = true;
        }
        if(Input.GetKeyDown(shotKey))
        {
            readyShot = true;
        }

       

        if (Input.GetKey( jumpKey ))
		{
            if (_velocity.y >= 0 && !_controller.isGrounded)
            {
                _velocity.y -= gravity / JumpDrag * Time.deltaTime;
            }

            if (_controller.isGrounded && Input.GetKeyDown(jumpKey))
            {
                _velocity.y = Mathf.Sqrt(2f * minJumpHeight * -gravity);
                //canJump = false;
            }
           
            
        }


        // apply gravity before moving
        _velocity.y += gravity * Time.deltaTime;

        if (bounceDamping)
        {
            if(normalizedHorizontalSpeed != 0)
            {
                bounceDamping = false;
            }
            else
            {
                _velocity.x = Mathf.Lerp(_velocity.x, 0, Time.deltaTime * bounceDampingSpeed);
            }

        }
        if(!bounceDamping)
        {
            var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
            _velocity.x = Mathf.Lerp(_velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor);
        }

        if (readyDash)
        {
            CurrentFrame++;
            if(CurrentFrame == delayFrame)
            {
                readyDash = false;
                CurrentFrame = 0;
                BeginNormalDashState();
                return;
            }
        }

        if(readyShot)
        {
            CurrentFrame++;
            if (CurrentFrame == delayFrame)
            {
                readyShot = false;
                CurrentFrame = 0;
                Shot();
                //return;
            }
        }

        _controller.move( _velocity * Time.deltaTime );

		// grab our current _velocity to use as a base for all calculations
		_velocity = _controller.velocity;
	}

    void Jump(float jumpHeight)
    {
        _velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
    }

    void ResetDashTime()
    {
        dashRestTime = dashMaxTimes;
    }

    void BeginNormalDashState()
    {
        dashRestTime--;
        dashState = true;
        _velocity = dashDirection * normalDash.dashSpeed;
        currentDash = normalDash;
        Dash();
    }


    void Dash()
    {
        Vector3 dashDelta = _velocity * Time.deltaTime;
        _controller.move(dashDelta);

        hasDashDistance += Vector3.Magnitude(dashDelta);

        if (hasDashDistance >= currentDash.dashMaxDistance)
        {
            if (_velocity.y > 0)
            {
                _velocity.y = -currentDash.upDashDamping * gravity * Time.deltaTime;
            }
            else if (_velocity.y < 0)
            {
                _velocity.y = currentDash.downDashDamping * gravity * Time.deltaTime;
            }

            hasDashDistance = 0;
            dashState = false;

        }
    }

    void BeginPauseState()
    {
        pauseState = true;
    }

    void BeginBounceState(bounceDirection dir)
    {
        if(!dashState)
        {
            return;
        }
        hasDashDistance = 0;
        dashState = false;
        bounceOnWallState = true;
        bounceDir = dir;
        int direction = dir == bounceDirection.right ? 1 : -1;
        //_velocity.x = Mathf.Sqrt(bounceForceX * bounceDragX * 2) * direction; bounceForceX为3左右
        //_velocity.y = Mathf.Sqrt(bounceForceY * -gravity * 2);bounceForceY为2左右
        _velocity.x = bounceForceX * direction;
        _velocity.y = bounceForceY;
    }

    void Bounce()
    {       
        _controller.move(_velocity*Time.deltaTime);
        int direction = bounceDir == bounceDirection.right ? 1 : -1;
       _velocity.x -= bounceDragX * direction * Time.deltaTime;
        _velocity.y += gravity * Time.deltaTime;

        //bounce state over
        if(_velocity.x * direction <= runSpeed )
        {
            bounceOnWallState = false;
            bounceDamping = true;
        }
    }

    public void BeginSuperDashState(int dirMark)
    {
        Debug.Log("haha");
        Vector2 dashDir = Vector2.zero;
        switch(dirMark)
        {
            case 1:dashDir = new Vector2(-1, 1);break;
            case 2:dashDir = new Vector2(-1, 0);break;
            case 3:dashDir = new Vector2(-1, -1);break;
            case 4:dashDir = new Vector2(1, 1);break;
            case 5: dashDir = new Vector2(1, 0); break;
            case 6: dashDir = new Vector2(1, -1); break;
        }
        pauseState = false;
        dashState = true;
        _velocity = dashDir * superDash.dashSpeed;
        currentDash = superDash;

        if(rightArrow.gameObject.activeSelf)
            rightArrow.gameObject.SetActive(false);

        if (leftArrow.gameObject.activeSelf)
            leftArrow.gameObject.SetActive(false);

    }

    void SetArrowPos(Transform arrow)
    {
        arrow.position = transform.position + Vector3.back * 2 + Vector3.up * 0.3f;
        arrow.gameObject.SetActive(true);
    }

    void Shot()
    {
        Quaternion rotate = Quaternion.identity;
        if (dashDirection == Vector2.right)
        {
            rotate = Quaternion.Euler(0, 0, -90);
        }
        else if (dashDirection == new Vector2(1, 1))
        {
            rotate = Quaternion.Euler(0, 0, -45);
        }
        else if (dashDirection == new Vector2(1, -1))
        {
            rotate = Quaternion.Euler(0, 0, -135);
        }
        else if (dashDirection == new Vector2(-1, 1))
        {
            rotate = Quaternion.Euler(0, 0, 45);
        }
        else if (dashDirection == Vector2.left)
        {
            rotate = Quaternion.Euler(0, 0, 90);
        }
        else if (dashDirection == new Vector2(-1, -1))
        {
            rotate = Quaternion.Euler(0, 0, 135);
        }
        FreezeBullet bullet = Instantiate(bulletPrefab, transform.position, rotate).GetComponent<FreezeBullet>();
        bullet.Init(dashDirection);
        //Game
    }

    void Die()
    {
        gameObject.SetActive(false);
    }

    void Relive(Vector3 relivePos)
    {
        transform.position = relivePos;
        gameObject.SetActive(true);
    }
}
