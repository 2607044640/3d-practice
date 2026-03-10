# Design Document: Interface-Based Input System

## Overview

This design refactors the current abstract class-based input architecture to an interface-based architecture that leverages Godot.Composition's native dependency injection capabilities. The core insight driving this refactoring is that Godot.Composition's ComponentContainer only registers concrete classes and interfaces (not base classes) in its dependency resolution dictionary, making interface-based dependencies the idiomatic approach.

The refactoring eliminates manual component lookup code, reduces boilerplate, and aligns with standard C# interface-based programming patterns. All existing functionality (player movement, jumping, rotation, animations) will be preserved while the underlying architecture becomes cleaner and more maintainable.

## Architecture

### Current Architecture (Abstract Class-Based)

```
BaseInputComponent (abstract class)
    ├── PlayerInputComponent (concrete implementation)
    └── AIInputComponent (future implementation)

Dependent Components:
    ├── MovementComponent
    │   └── Manual lookup in OnEntityReady()
    ├── CharacterRotationComponent
    │   └── Manual lookup in OnEntityReady()
    └── AnimationControllerComponent
        └── Manual lookup in OnEntityReady()
```

**Problems:**
- `[ComponentDependency(typeof(BaseInputComponent))]` doesn't work (base classes not registered)
- Requires manual component lookup via `ComponentExtensions.FindAndSubscribeInput()`
- Boilerplate code in every dependent component's `OnEntityReady()`
- Not idiomatic C# (abstract classes for contracts instead of interfaces)

### New Architecture (Interface-Based)

```
IEntityInput (interface : IComponent)
    └── PlayerInputComponent (concrete implementation)

Dependent Components:
    ├── MovementComponent
    │   └── [ComponentDependency(typeof(IEntityInput))] → Auto-injected
    ├── CharacterRotationComponent
    │   └── [ComponentDependency(typeof(IEntityInput))] → Auto-injected
    └── AnimationControllerComponent
        └── [ComponentDependency(typeof(IEntityInput))] → Auto-injected
```

**Benefits:**
- Automatic dependency injection works (interfaces are registered)
- Zero manual lookup code
- Cleaner, more maintainable codebase
- Idiomatic C# interface-based programming
- Easier to add new input implementations (AI, replay, etc.)

## Components and Interfaces

### IEntityInput Interface

**Purpose:** Define the contract for all input components in the system.

**Location:** `addons/CoreComponents/IEntityInput.cs`

**Interface Definition:**
```csharp
using Godot;
using Godot.Composition;
using System;

/// <summary>
/// Interface for entity input components.
/// Inherits from IComponent to enable Godot.Composition dependency injection.
/// </summary>
public interface IEntityInput : IComponent
{
    /// <summary>
    /// Triggered when movement input is detected.
    /// Vector2 represents direction (normalized or raw based on implementation).
    /// </summary>
    event Action<Vector2> OnMovementInput;
    
    /// <summary>
    /// Triggered when jump input is pressed (not held).
    /// </summary>
    event Action OnJumpJustPressed;
    
    /// <summary>
    /// Controls whether input events should be triggered.
    /// When false, implementations should not fire any events.
    /// </summary>
    bool InputEnabled { get; set; }
}
```

**Key Design Decisions:**
- Inherits `IComponent` to register with Godot.Composition's dependency system
- Uses C# events (`Action<T>`) for decoupled communication
- `InputEnabled` property allows runtime enable/disable without unsubscribing
- No methods defined - pure event-driven interface

### PlayerInputComponent

**Purpose:** Concrete implementation of IEntityInput for player keyboard/gamepad input.

**Location:** `addons/CoreComponents/PlayerInputComponent.cs`

**Implementation Pattern:**
```csharp
using Godot;
using Godot.Composition;
using System;

[GlobalClass]
[Component(typeof(CharacterBody3D))]
public partial class PlayerInputComponent : Node, IEntityInput
{
    // Interface implementation
    public event Action<Vector2> OnMovementInput;
    public event Action OnJumpJustPressed;
    
    [Export] public bool InputEnabled { get; set; } = true;
    
    public override void _Ready()
    {
        InitializeComponent();
    }
    
    public override void _Process(double delta)
    {
        if (!InputEnabled) return;
        
        // Capture movement input
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        if (inputDir != Vector2.Zero)
        {
            OnMovementInput?.Invoke(inputDir);
        }
        
        // Capture jump input
        if (Input.IsActionJustPressed("jump"))
        {
            OnJumpJustPressed?.Invoke();
        }
    }
}
```

**Key Design Decisions:**
- Implements `IEntityInput` interface explicitly
- Marked with `[GlobalClass]` for Godot editor visibility
- Marked with `[Component(typeof(CharacterBody3D))]` for Godot.Composition
- Calls `InitializeComponent()` in `_Ready()` (required by Godot.Composition)
- Uses null-conditional operator (`?.`) for safe event invocation
- Checks `InputEnabled` before processing input

### MovementComponent

**Purpose:** Handle character physics-based movement and jumping.

**Location:** `addons/CoreComponents/MovementComponent.cs`

**Refactored Implementation:**
```csharp
using Godot;
using Godot.Composition;
using System;

[GlobalClass]
[Component(typeof(CharacterBody3D))]
[ComponentDependency(typeof(IEntityInput))] // Auto-injection!
public partial class MovementComponent : Node
{
    [Export] public float Speed { get; set; } = 5.0f;
    [Export] public float JumpVelocity { get; set; } = 4.5f;
    [Export] public float Gravity { get; set; } = 9.8f;
    [Export] public Camera3D Camera { get; set; }
    
    // Auto-generated by Godot.Composition
    private IEntityInput entityInput;
    
    private Vector2 _currentInputDirection;
    
    public override void _Ready()
    {
        InitializeComponent();
    }
    
    public void OnEntityReady()
    {
        // Direct subscription - no manual lookup!
        entityInput.OnMovementInput += HandleMovementInput;
        entityInput.OnJumpJustPressed += HandleJumpInput;
    }
    
    public override void _ExitTree()
    {
        // Clean up subscriptions
        if (entityInput != null)
        {
            entityInput.OnMovementInput -= HandleMovementInput;
            entityInput.OnJumpJustPressed -= HandleJumpInput;
        }
    }
    
    private void HandleMovementInput(Vector2 direction)
    {
        _currentInputDirection = direction;
    }
    
    private void HandleJumpInput()
    {
        if (parent.IsOnFloor())
        {
            Vector3 velocity = parent.Velocity;
            velocity.Y = JumpVelocity;
            parent.Velocity = velocity;
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = parent.Velocity;
        
        // Apply gravity
        if (!parent.IsOnFloor())
        {
            velocity.Y -= Gravity * (float)delta;
        }
        
        // Calculate movement direction relative to camera
        Vector3 direction = Vector3.Zero;
        if (Camera != null)
        {
            Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z;
            Vector3 cameraRight = Camera.GlobalTransform.Basis.X;
            cameraForward.Y = 0;
            cameraRight.Y = 0;
            cameraForward = cameraForward.Normalized();
            cameraRight = cameraRight.Normalized();
            
            direction = (cameraRight * _currentInputDirection.X + cameraForward * _currentInputDirection.Y).Normalized();
        }
        
        // Apply movement
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }
        
        parent.Velocity = velocity;
        parent.MoveAndSlide();
    }
}
```

**Changes from Current Implementation:**
- Added `[ComponentDependency(typeof(IEntityInput))]` attribute
- Removed manual `FindAndSubscribeInput()` call
- Direct subscription to `entityInput` (auto-injected variable)
- Cleaner `OnEntityReady()` method (2 lines instead of 10+)

### CharacterRotationComponent

**Purpose:** Rotate character model to face movement direction.

**Location:** `addons/CoreComponents/CharacterRotationComponent.cs`

**Refactored Implementation:**
```csharp
using Godot;
using Godot.Composition;
using System;

[GlobalClass]
[Component(typeof(CharacterBody3D))]
[ComponentDependency(typeof(IEntityInput))] // Auto-injection!
public partial class CharacterRotationComponent : Node
{
    [Export] public NodePath CharacterModelPath { get; set; }
    [Export] public Camera3D Camera { get; set; }
    [Export] public float RotationSpeed { get; set; } = 10.0f;
    
    private Node3D _characterModel;
    private IEntityInput entityInput; // Auto-generated
    private Vector2 _currentInputDirection;
    
    public override void _Ready()
    {
        InitializeComponent();
        _characterModel = GetNode<Node3D>(CharacterModelPath);
    }
    
    public void OnEntityReady()
    {
        entityInput.OnMovementInput += HandleMovementInput;
    }
    
    public override void _ExitTree()
    {
        if (entityInput != null)
        {
            entityInput.OnMovementInput -= HandleMovementInput;
        }
    }
    
    private void HandleMovementInput(Vector2 direction)
    {
        _currentInputDirection = direction;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (_currentInputDirection == Vector2.Zero || _characterModel == null || Camera == null)
            return;
        
        // Calculate target direction relative to camera
        Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z;
        Vector3 cameraRight = Camera.GlobalTransform.Basis.X;
        cameraForward.Y = 0;
        cameraRight.Y = 0;
        cameraForward = cameraForward.Normalized();
        cameraRight = cameraRight.Normalized();
        
        Vector3 targetDirection = (cameraRight * _currentInputDirection.X + cameraForward * _currentInputDirection.Y).Normalized();
        
        if (targetDirection != Vector3.Zero)
        {
            // Smoothly rotate toward target direction
            Quaternion currentRotation = _characterModel.Quaternion;
            Quaternion targetRotation = new Basis(Vector3.Up, Mathf.Atan2(targetDirection.X, targetDirection.Z)).GetRotationQuaternion();
            _characterModel.Quaternion = currentRotation.Slerp(targetRotation, RotationSpeed * (float)delta);
        }
    }
}
```

**Changes from Current Implementation:**
- Added `[ComponentDependency(typeof(IEntityInput))]` attribute
- Removed manual component lookup
- Direct subscription to `entityInput`

### AnimationControllerComponent

**Purpose:** Control character animations based on movement state and input.

**Location:** `addons/CoreComponents/AnimationControllerComponent.cs`

**Refactored Implementation:**
```csharp
using Godot;
using Godot.Composition;
using System;

[GlobalClass]
[Component(typeof(CharacterBody3D))]
[ComponentDependency(typeof(IEntityInput))] // Auto-injection!
public partial class AnimationControllerComponent : Node
{
    [Export] public NodePath CharacterModelPath { get; set; }
    [Export] public NodePath AnimationPlayerPath { get; set; }
    [Export] public float AnimationBlendTime { get; set; } = 0.2f;
    [Export] public CharacterAnimationConfig AnimConfig { get; set; }
    
    private AnimationPlayer _animPlayer;
    private IEntityInput entityInput; // Auto-generated
    private bool _isJumping;
    
    public override void _Ready()
    {
        InitializeComponent();
        
        Node3D characterModel = GetNode<Node3D>(CharacterModelPath);
        _animPlayer = characterModel.GetNode<AnimationPlayer>(AnimationPlayerPath);
        
        if (AnimConfig != null)
        {
            AnimConfig.ApplyToAnimationPlayer(_animPlayer);
        }
    }
    
    public void OnEntityReady()
    {
        entityInput.OnMovementInput += HandleMovementInput;
        entityInput.OnJumpJustPressed += HandleJumpInput;
    }
    
    public override void _ExitTree()
    {
        if (entityInput != null)
        {
            entityInput.OnMovementInput -= HandleMovementInput;
            entityInput.OnJumpJustPressed -= HandleJumpInput;
        }
    }
    
    private void HandleMovementInput(Vector2 direction)
    {
        if (_isJumping) return;
        
        if (direction == Vector2.Zero)
        {
            PlayAnimation(AnimationNames.Idle);
        }
        else
        {
            float speed = direction.Length();
            if (speed > 0.8f)
            {
                PlayAnimation(AnimationNames.Run);
            }
            else
            {
                PlayAnimation(AnimationNames.Walk);
            }
        }
    }
    
    private void HandleJumpInput()
    {
        if (parent.IsOnFloor())
        {
            _isJumping = true;
            PlayAnimation(AnimationNames.JumpStart);
        }
    }
    
    private void PlayAnimation(string animName)
    {
        if (_animPlayer != null && _animPlayer.HasAnimation(animName))
        {
            _animPlayer.Play(animName, AnimationBlendTime);
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // Reset jumping flag when landing
        if (_isJumping && parent.IsOnFloor() && parent.Velocity.Y <= 0)
        {
            _isJumping = false;
            PlayAnimation(AnimationNames.Idle);
        }
    }
}
```

**Changes from Current Implementation:**
- Added `[ComponentDependency(typeof(IEntityInput))]` attribute
- Removed manual component lookup
- Direct subscription to `entityInput`

## Data Models

### Interface Contract

The `IEntityInput` interface defines the data contract:

```csharp
public interface IEntityInput : IComponent
{
    event Action<Vector2> OnMovementInput;  // Movement direction data
    event Action OnJumpJustPressed;         // Jump trigger (no data)
    bool InputEnabled { get; set; }         // State flag
}
```

**Data Flow:**
1. Input source (keyboard/gamepad) → PlayerInputComponent
2. PlayerInputComponent processes raw input → Triggers events with processed data
3. Events propagate to all subscribed components
4. Components update their internal state based on event data
5. Components apply changes in `_PhysicsProcess()`

### Event Data Types

- **OnMovementInput**: `Vector2`
  - X: Horizontal input (-1 to 1, left to right)
  - Y: Vertical input (-1 to 1, backward to forward)
  - Normalized or raw based on implementation choice

- **OnJumpJustPressed**: No parameters
  - Simple trigger event
  - Subscribers decide how to respond based on their state

### State Management

Each component maintains its own state:
- **MovementComponent**: `_currentInputDirection` (Vector2)
- **CharacterRotationComponent**: `_currentInputDirection` (Vector2)
- **AnimationControllerComponent**: `_isJumping` (bool)

This decentralized state management ensures components remain independent and testable.


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Dependency Injection Completeness

*For any* component with `[ComponentDependency(typeof(IEntityInput))]`, when `OnEntityReady()` is called, the `entityInput` variable shall be non-null and ready for event subscription.

**Validates: Requirements 2.1, 2.3**

### Property 2: Event Broadcasting to Multiple Subscribers

*For any* entity with multiple components depending on IEntityInput, when an input event is triggered, all subscribed components shall receive the event with identical data.

**Validates: Requirements 2.4**

### Property 3: Input Direction Mapping

*For any* valid input key combination (WASD), the PlayerInputComponent shall trigger OnMovementInput with a direction vector that correctly represents the input direction (normalized or raw based on implementation).

**Validates: Requirements 3.2**

### Property 4: Input Enable/Disable

*For any* input event while InputEnabled is false, the PlayerInputComponent shall not trigger any input events (OnMovementInput or OnJumpJustPressed).

**Validates: Requirements 3.4**

### Property 5: Movement Velocity Update

*For any* input direction vector, when OnMovementInput is triggered, the MovementComponent shall update the character's velocity to move in the direction relative to the camera orientation.

**Validates: Requirements 4.4**

### Property 6: Jump Velocity Application

*For any* character state where IsOnFloor() returns true, when OnJumpJustPressed is triggered, the MovementComponent shall set the vertical velocity to the configured JumpVelocity value.

**Validates: Requirements 4.5**

### Property 7: Event Unsubscription Cleanup

*For any* component that subscribes to IEntityInput events, when _ExitTree() is called, the component shall unsubscribe from all events, and subsequent event triggers shall not invoke the component's handlers.

**Validates: Requirements 4.6, 5.5, 6.6**

### Property 8: Character Rotation Toward Movement

*For any* non-zero input direction, when OnMovementInput is triggered, the CharacterRotationComponent shall rotate the character model toward the movement direction relative to camera orientation.

**Validates: Requirements 5.3**

### Property 9: Smooth Rotation Interpolation

*For any* rotation from current orientation to target orientation, the CharacterRotationComponent shall interpolate smoothly over multiple frames using the configured RotationSpeed, rather than snapping instantly.

**Validates: Requirements 5.4**

### Property 10: Animation State Transitions

*For any* input magnitude (zero, low, high), when OnMovementInput is triggered, the AnimationControllerComponent shall transition to the appropriate animation state (Idle for zero, Walk for low, Run for high).

**Validates: Requirements 6.4**

### Property 11: Backward Compatibility Preservation

*For any* existing game functionality (movement, jumping, rotation, animation), after refactoring to interface-based architecture, the behavior shall remain functionally equivalent to the pre-refactoring implementation.

**Validates: Requirements 7.5**

## Error Handling

### Compilation Errors

**Missing Interface Implementation:**
- **Error**: Class implements IEntityInput but doesn't provide all required members
- **Handling**: C# compiler will prevent compilation with clear error messages
- **Prevention**: Use IDE intellisense and compiler feedback during development

**Missing Dependency:**
- **Error**: Component declares `[ComponentDependency(typeof(IEntityInput))]` but no implementation exists in entity
- **Handling**: Godot.Composition logs error during InitializeEntity()
- **Prevention**: Ensure PlayerInputComponent (or other IEntityInput implementation) is added to entity scene

### Runtime Errors

**Null Reference on Event Invocation:**
- **Error**: Attempting to invoke event when no subscribers exist
- **Handling**: Use null-conditional operator (`?.`) for all event invocations
- **Example**: `OnMovementInput?.Invoke(direction);`
- **Prevention**: Always use `?.` pattern in event-triggering code

**Event Subscription Memory Leaks:**
- **Error**: Component destroyed but event subscriptions remain
- **Handling**: Implement `_ExitTree()` to unsubscribe from all events
- **Prevention**: Always pair event subscription with unsubscription
- **Pattern**:
```csharp
public void OnEntityReady()
{
    entityInput.OnMovementInput += HandleMovementInput;
}

public override void _ExitTree()
{
    if (entityInput != null)
    {
        entityInput.OnMovementInput -= HandleMovementInput;
    }
}
```

**Camera Reference Missing:**
- **Error**: MovementComponent or CharacterRotationComponent has null Camera reference
- **Handling**: Check for null before using Camera in calculations
- **Prevention**: Use `[Export]` for Camera and validate in editor

### Godot.Composition Specific Errors

**Dependency Injection Failure:**
- **Error**: `entityInput` is null in `OnEntityReady()`
- **Cause**: IEntityInput implementation not properly registered or missing `[Component]` attribute
- **Solution**: Verify PlayerInputComponent has `[Component(typeof(CharacterBody3D))]` and calls `InitializeComponent()`

**Interface Not Recognized:**
- **Error**: Dependency injection doesn't work for IEntityInput
- **Cause**: Interface doesn't inherit from `IComponent`
- **Solution**: Ensure `IEntityInput : IComponent` inheritance

## Testing Strategy

### Dual Testing Approach

This feature requires both unit testing and property-based testing for comprehensive coverage:

**Unit Tests:**
- Specific examples of input combinations (W, WA, WD, etc.)
- Edge cases (zero input, maximum input, rapid input changes)
- Integration points (component initialization, event subscription)
- Error conditions (missing dependencies, null references)

**Property Tests:**
- Universal properties across all possible inputs
- Randomized input sequences to catch edge cases
- State consistency across component lifecycle
- Event propagation correctness

### Property-Based Testing Configuration

**Testing Library:** Use NUnit with FsCheck (or similar property-based testing library for C#)

**Test Configuration:**
- Minimum 100 iterations per property test
- Each test tagged with feature name and property number
- Tag format: `[Property(Feature = "interface-based-input-system", Property = 1)]`

**Example Property Test Structure:**
```csharp
[Test]
[Property(Feature = "interface-based-input-system", Property = 3)]
public void InputDirectionMapping_AllValidInputs_ProducesCorrectVector()
{
    // Feature: interface-based-input-system, Property 3: Input Direction Mapping
    // For any valid input key combination, correct direction vector is produced
    
    Prop.ForAll<bool, bool, bool, bool>((w, a, s, d) =>
    {
        // Arrange: Create input component with specific key states
        var inputComponent = CreatePlayerInputComponent();
        Vector2 receivedDirection = Vector2.Zero;
        inputComponent.OnMovementInput += (dir) => receivedDirection = dir;
        
        // Act: Simulate input
        SimulateInput(inputComponent, w, a, s, d);
        
        // Assert: Verify direction matches expected
        Vector2 expected = CalculateExpectedDirection(w, a, s, d);
        return receivedDirection.IsEqualApprox(expected);
    }).QuickCheckThrowOnFailure();
}
```

### Unit Test Examples

**Example 1: Dependency Injection**
```csharp
[Test]
public void MovementComponent_OnEntityReady_EntityInputIsNotNull()
{
    // Arrange
    var entity = CreateTestEntity();
    var inputComponent = entity.AddComponent<PlayerInputComponent>();
    var movementComponent = entity.AddComponent<MovementComponent>();
    
    // Act
    entity.InitializeEntity();
    
    // Assert
    Assert.IsNotNull(movementComponent.entityInput);
}
```

**Example 2: Input Disabled**
```csharp
[Test]
public void PlayerInputComponent_InputDisabled_NoEventsTriggered()
{
    // Arrange
    var inputComponent = CreatePlayerInputComponent();
    inputComponent.InputEnabled = false;
    bool eventTriggered = false;
    inputComponent.OnMovementInput += (_) => eventTriggered = true;
    
    // Act
    SimulateInput(inputComponent, w: true, a: false, s: false, d: false);
    
    // Assert
    Assert.IsFalse(eventTriggered);
}
```

**Example 3: Event Unsubscription**
```csharp
[Test]
public void MovementComponent_ExitTree_UnsubscribesFromEvents()
{
    // Arrange
    var entity = CreateTestEntity();
    var inputComponent = entity.AddComponent<PlayerInputComponent>();
    var movementComponent = entity.AddComponent<MovementComponent>();
    entity.InitializeEntity();
    
    // Act
    movementComponent._ExitTree();
    bool eventReceived = false;
    // Trigger event after unsubscription
    inputComponent.OnMovementInput?.Invoke(Vector2.One);
    
    // Assert
    Assert.IsFalse(eventReceived); // Movement component should not respond
}
```

### Integration Testing

**Manual Testing Checklist:**
1. Load Player3D scene in Godot editor
2. Run game (F5)
3. Test WASD movement - character should move smoothly
4. Test Space jump - character should jump when on ground
5. Test character rotation - model should face movement direction
6. Test animations - Idle/Walk/Run/Jump animations should play correctly
7. Verify no console errors or warnings

**Automated Integration Tests:**
- Create test scenes with Player3D entity
- Simulate input sequences programmatically
- Verify final character state (position, rotation, animation)
- Compare behavior with pre-refactoring baseline

### Test Coverage Goals

- **Unit Tests**: 80%+ code coverage for all components
- **Property Tests**: All 11 correctness properties implemented
- **Integration Tests**: All 5 manual test scenarios automated
- **Regression Tests**: Baseline comparison with pre-refactoring behavior

### Continuous Testing

**During Development:**
1. Run unit tests after each component modification
2. Run property tests after completing each component
3. Run integration tests after wiring components together
4. Compile and run game to verify functionality

**Before Commit:**
1. Full test suite passes (unit + property + integration)
2. No compiler warnings
3. Manual testing checklist completed
4. Documentation updated

