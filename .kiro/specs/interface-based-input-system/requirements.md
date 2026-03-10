# Requirements Document

## Introduction

This document specifies the requirements for refactoring the current abstract class-based input system to an interface-based architecture. The refactoring addresses a fundamental limitation in the Godot.Composition plugin: the ComponentContainer only registers concrete classes and interfaces (not base classes) for dependency injection. By adopting an interface-based approach, we enable automatic dependency injection, eliminate manual component lookup code, and align with standard C# architectural patterns.

## Glossary

- **IEntityInput**: The interface defining the input contract for all input components
- **PlayerInputComponent**: Concrete implementation of IEntityInput for player keyboard/gamepad input
- **Godot.Composition**: Third-party plugin providing component-based architecture for Godot
- **ComponentDependency**: Attribute used to declare component dependencies for automatic injection
- **IComponent**: Base interface from Godot.Composition that all component interfaces must inherit
- **Input_System**: The collection of components responsible for capturing and distributing input events
- **Dependent_Components**: Components that consume input events (MovementComponent, CharacterRotationComponent, AnimationControllerComponent)

## Requirements

### Requirement 1: Interface-Based Input Contract

**User Story:** As a developer, I want a clear interface contract for input components, so that I can implement different input sources while maintaining compatibility with dependent components.

#### Acceptance Criteria

1. THE Input_System SHALL define an IEntityInput interface that inherits from Godot.Composition.IComponent
2. THE IEntityInput interface SHALL declare an OnMovementInput event with Vector2 parameter
3. THE IEntityInput interface SHALL declare an OnJumpJustPressed event with no parameters
4. THE IEntityInput interface SHALL declare an InputEnabled property of type bool
5. WHEN a class implements IEntityInput, THE Input_System SHALL enforce implementation of all declared members

### Requirement 2: Automatic Dependency Injection

**User Story:** As a developer, I want components to automatically receive their input dependencies, so that I don't need to write manual component lookup code.

#### Acceptance Criteria

1. WHEN a component declares [ComponentDependency(typeof(IEntityInput))], THE Godot.Composition plugin SHALL automatically inject the IEntityInput instance
2. THE injected variable SHALL be named "entityInput" following the plugin's lowercase-first-letter convention
3. WHEN OnEntityReady() is called, THE entityInput variable SHALL be non-null and ready for event subscription
4. THE Input_System SHALL support multiple components depending on the same IEntityInput instance
5. IF no IEntityInput implementation exists in the entity, THEN THE Godot.Composition plugin SHALL log an error during initialization

### Requirement 3: Player Input Implementation

**User Story:** As a player, I want my keyboard and gamepad inputs to control the character, so that I can interact with the game.

#### Acceptance Criteria

1. THE PlayerInputComponent SHALL implement the IEntityInput interface
2. WHEN the player presses movement keys (WASD), THE PlayerInputComponent SHALL trigger OnMovementInput with the corresponding direction vector
3. WHEN the player presses the jump key (Space), THE PlayerInputComponent SHALL trigger OnJumpJustPressed
4. WHILE InputEnabled is false, THE PlayerInputComponent SHALL not trigger any input events
5. THE PlayerInputComponent SHALL be marked with [GlobalClass] and [Component(typeof(CharacterBody3D))] attributes

### Requirement 4: Movement Component Integration

**User Story:** As a developer, I want the movement component to respond to input events, so that characters can move and jump based on input.

#### Acceptance Criteria

1. THE MovementComponent SHALL declare [ComponentDependency(typeof(IEntityInput))]
2. WHEN OnEntityReady() is called, THE MovementComponent SHALL subscribe to entityInput.OnMovementInput
3. WHEN OnEntityReady() is called, THE MovementComponent SHALL subscribe to entityInput.OnJumpJustPressed
4. WHEN OnMovementInput is triggered, THE MovementComponent SHALL update the character's velocity based on the input direction
5. WHEN OnJumpJustPressed is triggered and the character is on the ground, THE MovementComponent SHALL apply jump velocity
6. WHEN the component is destroyed, THE MovementComponent SHALL unsubscribe from all input events in _ExitTree()

### Requirement 5: Character Rotation Integration

**User Story:** As a player, I want my character to face the direction of movement, so that the character orientation matches my input.

#### Acceptance Criteria

1. THE CharacterRotationComponent SHALL declare [ComponentDependency(typeof(IEntityInput))]
2. WHEN OnEntityReady() is called, THE CharacterRotationComponent SHALL subscribe to entityInput.OnMovementInput
3. WHEN OnMovementInput is triggered with non-zero direction, THE CharacterRotationComponent SHALL rotate the character model toward the movement direction
4. THE rotation SHALL be smoothed using the configured RotationSpeed
5. WHEN the component is destroyed, THE CharacterRotationComponent SHALL unsubscribe from input events in _ExitTree()

### Requirement 6: Animation Controller Integration

**User Story:** As a player, I want character animations to respond to my inputs, so that the character visually reflects my actions.

#### Acceptance Criteria

1. THE AnimationControllerComponent SHALL declare [ComponentDependency(typeof(IEntityInput))]
2. WHEN OnEntityReady() is called, THE AnimationControllerComponent SHALL subscribe to entityInput.OnMovementInput
3. WHEN OnEntityReady() is called, THE AnimationControllerComponent SHALL subscribe to entityInput.OnJumpJustPressed
4. WHEN OnMovementInput is triggered, THE AnimationControllerComponent SHALL transition to appropriate movement animations (Idle/Walk/Run)
5. WHEN OnJumpJustPressed is triggered, THE AnimationControllerComponent SHALL play jump animations
6. WHEN the component is destroyed, THE AnimationControllerComponent SHALL unsubscribe from input events in _ExitTree()

### Requirement 7: Legacy Code Removal

**User Story:** As a developer, I want to remove obsolete code, so that the codebase remains clean and maintainable.

#### Acceptance Criteria

1. WHEN the interface-based system is implemented, THE Input_System SHALL delete BaseInputComponent.cs
2. WHEN the interface-based system is implemented, THE Input_System SHALL delete ComponentExtensions.cs
3. THE Input_System SHALL remove all manual component lookup code from OnEntityReady() methods
4. THE Input_System SHALL remove all calls to FindAndSubscribeInput() helper methods
5. THE Input_System SHALL maintain backward compatibility with existing game functionality

### Requirement 8: Documentation Updates

**User Story:** As a developer, I want updated documentation, so that I understand how to use the new interface-based system.

#### Acceptance Criteria

1. THE documentation SHALL update README.md to reflect the interface-based architecture
2. THE documentation SHALL update ARCHITECTURE.md with interface design patterns
3. THE documentation SHALL update QUICK_START.md with IEntityInput usage examples
4. THE documentation SHALL update MIGRATION_GUIDE.md with migration steps from abstract class to interface
5. THE documentation SHALL update all code examples to use IEntityInput instead of BaseInputComponent

### Requirement 9: Compilation and Runtime Verification

**User Story:** As a developer, I want the refactored system to compile and run correctly, so that I can verify the changes work as expected.

#### Acceptance Criteria

1. WHEN the refactoring is complete, THE Input_System SHALL compile without errors using `dotnet build`
2. WHEN the game runs, THE player character SHALL move in response to WASD input
3. WHEN the game runs, THE player character SHALL jump in response to Space input
4. WHEN the game runs, THE player character SHALL rotate to face movement direction
5. WHEN the game runs, THE player character animations SHALL play correctly based on input
