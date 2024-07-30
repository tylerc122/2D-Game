using Godot;
using System;
using System.Collections;
/// <summary>
/// SO I DONT FORGET HOW THIS WORKS:
/// When an attack is chosen, the state transitions from 'Idle' to 'Attack'
/// Each specific attack method will set the 'currentAttack' and then play the associated animation
/// When the animation plays, it will call the 'PerformAttack' method, which will (eventually) execute the logic of the attack
/// Then when the animation finishes, 'OnAnimationFinished' is called and the boss state will go back to idle.
/// Moreover, when the bosses health reaches 25% or under, (s)he will enter an enraged mode, we can just speed up attacks and/or make them do more damage.
/// </summary>

public partial class Boss_1 : CharacterBody2D
{
	// Stores boss states.
	enum BossState { Idle, Attack, Enraged }
	// Initially in Idle state.
	private BossState state = BossState.Idle;

	// Basic attributes.
	public int MaxHealth = 100;
	private int health;
	private float attackTimer = 0;

	float enragedDamageMultiplier = 2.0f;
	float enragedSpeedMultiplier = 1.5f;

	// Nodes
	private AnimatedSprite2D animatedSprite;
	private Area2D attackArea;
	private Ram ram;

	// Random variable for choosing attacks.
	private Random random = new Random();
	// In charge of storing the boss's currentAttack.
	private String currentAttack;

	public override void _Ready()
	{
		health = MaxHealth;

		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		attackArea = GetNode<Area2D>("Area2D");

		ram = GetNode<Ram>("Ram");

		animatedSprite.Connect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
	}
	public override void _PhysicsProcess(double delta)
	{
		// Switch expression based on the current state of the boss.
		switch (state)
		{
			// If in an Idle state
			case BossState.Idle:
				// Check if attackTimer is expired
				if (attackTimer <= 0)
				{
					// If so, choose an attack to do.
					// Note: I'm a little worried that this will be executed too fast/slow, not convinced a timer will be smooth enough.
					// Depending on how much damage ram is doing, I believe we'll have to adjust this.
					// If too fast, possible to maybe have another random case, maybe every 1/2 times he'll attack? idk
					// If too slow, we can always reduce the timer or some other robust solution, will think on it.
					ChooseAttack();
				}
				else
				{
					// If the attack timer isn't up, we can just reduce it.
					attackTimer -= (float)delta;
				}
				break;

			// If in Attack state
			case BossState.Attack:
				// Perform the attack.
				PerformAttack(1.0f, 1.0f);
				break;

			// If in an Enraged State
			case BossState.Enraged:
				PerformAttack(enragedDamageMultiplier, enragedSpeedMultiplier);
				break;
		}
	}
	/// Don't think I need to say what this will do.
	private void ChooseAttack()
	{
		// May need to adjust delta/speed, again, have to see in game.
		// Either way we need to move towards ram before an attack is chosen.
		MoveTowardsRam(100.0f, 0.1f);

		// We should only choose, execute, and perform the attack once Ram is close enough.
		// Range is subject to change.
		if (IsRamInAttackRange(200.0f))
		{
			// Make use of our random var that we initialized earlier.
			int attackChoice = random.Next(4);
			// Switch expression. Will chooose attack based on number.
			switch (attackChoice)
			{
				case 0:
					ExecuteAttack("SlamAttack", "SlamAttack");
					break;
				case 1:
					ExecuteAttack("BoulderThrow", "BoulderThrow");
					break;
				case 2:
					ExecuteAttack("StompAttack", "StompAttack");
					break;
				case 3:
					ExecuteAttack("ChargeAttack", "ChargeAttack");
					break;
			}
			// Reset attack timer once attack is performed.
			attackTimer = 2;
		}
	}
	/// Executes the attack by setting current attack, playing anim, & calling PerformAttack()
	/// @param attackName the name of the attack chosen from ChooseAttack()
	/// @param animationName the name of the animation corresponding with the attack.
	private void ExecuteAttack(string attackName, string animationName)
	{
		GD.Print(attackName);

		currentAttack = attackName;

		animatedSprite.Play(animationName);

		PerformAttack(1.0f, 1.0f);
	}
	// Performs the logic of the attack based on the currentAttack.
	private void PerformAttack(float damageMultiplier, float speedMultiplier)
	{
		// Since we now have the boss moving towards ram before attacking,
		// we need them to stop when they do the attack.
		Velocity = Vector2.Zero;
		switch (currentAttack)
		{
			case "SlamAttack":
				SlamAttackLogic(damageMultiplier);
				break;
			case "BoulderThrow":
				BoulderThrowLogic(damageMultiplier);
				break;
			case "StompAttack":
				StompAttackLogic(damageMultiplier);
				break;
			case "ChargeAttack":
				ChargeAttackLogic(damageMultiplier);
				break;
		}
	}

	/// Slam attack.
	private void SlamAttackLogic(float damageMultiplier)
	{
		// If Ram is range for the intial slamdown
		if (IsRamInAttackRange(50.0f))
		{
			// He takes 20 damage.
			ram.TakeDamage((int)(20 * damageMultiplier));
		}
		// Either way, the shockwave will be created.
		CreateShockwave();
	}
	// The actual creation of the shockwave from the slam.
	private void CreateShockwave()
	{
		// Loads a resource form the shockwave scene, we must cast to a 'PackedScene' since we'll get a 'Resource' object if we don't.
		PackedScene shockwaveScene = (PackedScene)ResourceLoader.Load("res://shockwave.tscn)");
		// Succesfully loaded?
		if (shockwaveScene != null)
		{
			// If yes, create an instance of the PackedScene. Casting to Area2D
			Area2D shockwaveInstance = (Area2D)shockwaveScene.Instantiate();

			// This will make sure that the shockwave starts where the boss is.
			shockwaveInstance.Position = GlobalPosition;

			// Gets the parent of the Boss_1 node, should just be the root node (Node2D)
			// Then it'll add the instance of the shockwave as a child of the parent, making it active & visible in game.
			GetParent().AddChild(shockwaveInstance);

			Vector2 directionToRam = (ram.GlobalPosition - GlobalPosition).Normalized();

			// We then call StartShockwave() in shockwave.cs which starts the actual movement of the shockwave. 
			shockwaveInstance.Call("StartShockwave", directionToRam);
		}
	}
	// Boulder throw attack.
	private void BoulderThrowLogic(float damageMultiplier)
	{
		// Same thing as shockwave.
		PackedScene boulderScene = (PackedScene)ResourceLoader.Load("res://boulder.tscn");

		// Make sure loaded properly.
		if (boulderScene != null)
		{
			// Create an instance of the boulder, unlike the shockwave, we need this to be a RigidBody2D since we're dealing with physics & gravity.
			RigidBody2D boulderInstance = (RigidBody2D)boulderScene.Instantiate();

			// Starts at the boss.
			boulderInstance.GlobalPosition = GlobalPosition;

			// Make it visible and active in game.
			GetParent().AddChild(boulderInstance);

			// Initialize & assign directionToRam, need to make sure we're throwing boulder at ram.
			Vector2 directionToRam = (ram.GlobalPosition - GlobalPosition).Normalized();

			// Speed of boulder.
			float boulderSpeed = 300.0f;

			// Sets velocity of boulder.
			boulderInstance.LinearVelocity = directionToRam * boulderSpeed;
			// NOTE: we can change the logic of this to have an arc instead of it just going straight across the screen, we'll
			// see how it looks in game then adjust if necessary:
			// CODE IF WE WANT AN ARC/GRAVITY PHYSICS:
			// float angle = Mathf.Atan2(directionToRam.y, directionToRam.x); // Calculates angle towards Ram.
			// float initialSpeedx = boulderSpeed * Mathf.Cos(angle);
			// float initialSpeedy = boulderSpeed * Mathf.Sin(angle) - 50.0f;

		}
	}

	/// Stomp attack.
	private void StompAttackLogic(float damageMultiplier)
	{
		// Check if ram is in range.
		if (IsRamInAttackRange(100.0f))
		{
			// If he is, he'll take 15 damage.
			ram.TakeDamage((int)(15 * damageMultiplier));
			// He will also be knocked back
			Vector2 knockbackDirection = (ram.GlobalPosition - GlobalPosition).Normalized();
			ram.ApplyKnockback(knockbackDirection, 200.0f);

		}
		// Then we create the stomping effect.
		CreateStompEffect();
	}

	/// Similar to all other attacks. However, need to add animation later.
	private void CreateStompEffect()
	{
		PackedScene stompScene = (PackedScene)ResourceLoader.Load("res://stomp.tscn");
		if (stompScene != null)
		{
			Area2D stompInstance = (Area2D)stompScene.Instantiate();

			stompInstance.Position = GlobalPosition;

			GetParent().AddChild(stompInstance);

			stompInstance.Call("StartStompEffect");
		}
	}
	private void ChargeAttackLogic(float damageMultiplier)
	{
		float chargeSpeed = 500.0f;

		MoveTowardsRam(chargeSpeed, (float)GetProcessDeltaTime());

		if (IsRamInAttackRange(30.0f))
		{
			ram.TakeDamage((int)(25 * damageMultiplier));
		}
	}
	/// Called when we want to check if ram is in the boss's attack range
	/// @param attackRange the range of the specific attack.
	/// @return bool the truth value of whether or not ram is within range or not.
	private bool IsRamInAttackRange(float attackRange)
	{
		return (ram.GlobalPosition - GlobalPosition).Length() <= attackRange;
	}
	// Called when Ram performs a successful attack, currently not in use, but self explanatory.
	public void OnHealthChanged(int newHealth)
	{
		health = newHealth;

		if (health <= 25)
		{
			state = BossState.Enraged;
		}
		else if (health <= 0)
		{
			Die();
		}
	}
	/// Called when the boss die.
	private void Die()
	{
		GD.Print("Boss Died");
	}

	// Called when an attacking(?) animation is carried out in full.
	private void OnAnimationFinished()
	{
		// Sets state back to idle if in attack
		// Note: A little confused by my own code, in what case would an animation be carried out while not in Attack mode?
		// Furthermore, do we not set the bosses state back to idle when we're in an enraged state? 
		// I'll revisit this tmrw.
		if (state == BossState.Attack)
		{
			state = BossState.Idle;
		}
	}

	private void MoveTowardsRam(float speed, float delta)
	{
		Vector2 directionToRam = (ram.GlobalPosition - GlobalPosition).Normalized();
		Velocity = directionToRam * speed;
		MoveAndSlide();
	}
}
