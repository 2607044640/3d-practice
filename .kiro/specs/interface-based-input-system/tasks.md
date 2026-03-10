# Implementation Plan: Interface-Based Input System

## Overview

This implementation plan refactors the abstract class-based input system to an interface-based architecture. The refactoring enables automatic dependency injection through Godot.Composition, eliminates manual component lookup code, and aligns with standard C# interface-based programming patterns. All tasks build incrementally, with testing integrated throughout to catch errors early.

## Tasks

- [ ] 1. Create IEntityInput interface
  - Create `addons/CoreComponents/IEntityInput.cs`
  - Define interface inheriting from `Godot.Composition.IComponent`
  - Declare `OnMovementInput` event with `Vector2` parameter
  - Declare `OnJumpJustPressed` event with no parameters
  - Declare `InputEnabled` property of type `bool`
  - Add XML documentation comments for all members
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [ ] 2. Refactor PlayerInputComponent to implement IEntityInput
  - [ ] 2.1 Update PlayerInputComponent class signature
    - Change from `public partial class PlayerInputComponent : BaseInputComponent`
    - To `public partial class PlayerInputComponent : Node, IEntityInput`
    - Keep `[GlobalClass]` and `[Component(typeof(CharacterBody3D))]` attributes
    - _Requirements: 3.1, 3.5_
  
  - [ ] 2.2 Implement IEntityInput interface members
    - Implement `OnMovementInput` event
    - Implement `OnJumpJustPressed` event
    - Implement `InputEnabled` property with `[Export]` attribute
    - Ensure `_Ready()` calls `InitializeComponent()`
    - _Requirements: 3.1_
  
  - [ ] 2.3 Update input processing logic
    - Verify `_Process()` checks `InputEnabled` before processing
    - Verify movement input triggers `OnMovementInput?.Invoke(direction)`
    - Verify jump input triggers `OnJumpJustPressed?.Invoke()`
    - Use null-conditional operator for all event invocations
    - _Requirements: 3.2, 3.3, 3.4_
  
  - [ ] 2.4 Write property test for input enable/disable
    - **Property 4: Input Enable/Disable**
    - **Validates: Requirements 3.4**
    - Test that no events fire when InputEnabled is false
    - Run 100+ iterations with random input combinations

- [ ] 3. Update MovementComponent for automatic dependency injection
  - [ ] 3.1 Add ComponentDependency attribute
    - Add `[ComponentDependency(typeof(IEntityInput))]` to class
    - Remove any manual component lookup code from `OnEntityReady()`
    - _Requirements: 4.1_
  
  - [ ] 3.2 Update event subscription in OnEntityReady()
    - Subscribe to `entityInput.OnMovementInput += HandleMovementInput`
    - Subscribe to `entityInput.OnJumpJustPressed += HandleJumpInput`
    - Remove any calls to `FindAndSubscribeInput()` or similar helpers
    - _Requirements: 4.2, 4.3_
  
  - [ ] 3.3 Implement event unsubscription in _ExitTree()
    - Check `if (entityInput != null)` before unsubscribing
    - Unsubscribe from `OnMovementInput`
    - Unsubscribe from `OnJumpJustPressed`
    - _Requirements: 4.6_
  
  - [ ] 3.4 Write property test for movement velocity update
    - **Property 5: Movement Velocity Update**
    - **Validates: Requirements 4.4**
    - Test that velocity updates correctly for any input direction
    - Run 100+ iterations with random direction vectors
  
  - [ ] 3.5 Write property test for jump velocity application
    - **Property 6: Jump Velocity Application**
    - **Validates: Requirements 4.5**
    - Test that jump velocity is applied when on ground
    - Run 100+ iterations with random character states

- [ ] 4. Update CharacterRotationComponent for automatic dependency injection
  - [ ] 4.1 Add ComponentDependency attribute
    - Add `[ComponentDependency(typeof(IEntityInput))]` to class
    - Remove manual component lookup code
    - _Requirements: 5.1_
  
  - [ ] 4.2 Update event subscription in OnEntityReady()
    - Subscribe to `entityInput.OnMovementInput += HandleMovementInput`
    - Remove calls to helper methods
    - _Requirements: 5.2_
  
  - [ ] 4.3 Implement event unsubscription in _ExitTree()
    - Check `if (entityInput != null)` before unsubscribing
    - Unsubscribe from `OnMovementInput`
    - _Requirements: 5.5_
  
  - [ ] 4.4 Write property test for character rotation
    - **Property 8: Character Rotation Toward Movement**
    - **Validates: Requirements 5.3**
    - Test that character rotates toward movement direction
    - Run 100+ iterations with random input directions
  
  - [ ] 4.5 Write property test for smooth rotation
    - **Property 9: Smooth Rotation Interpolation**
    - **Validates: Requirements 5.4**
    - Test that rotation interpolates smoothly over multiple frames
    - Verify no instant snapping occurs

- [ ] 5. Update AnimationControllerComponent for automatic dependency injection
  - [ ] 5.1 Add ComponentDependency attribute
    - Add `[ComponentDependency(typeof(IEntityInput))]` to class
    - Remove manual component lookup code
    - _Requirements: 6.1_
  
  - [ ] 5.2 Update event subscription in OnEntityReady()
    - Subscribe to `entityInput.OnMovementInput += HandleMovementInput`
    - Subscribe to `entityInput.OnJumpJustPressed += HandleJumpInput`
    - Remove helper method calls
    - _Requirements: 6.2, 6.3_
  
  - [ ] 5.3 Implement event unsubscription in _ExitTree()
    - Check `if (entityInput != null)` before unsubscribing
    - Unsubscribe from both events
    - _Requirements: 6.6_
  
  - [ ] 5.4 Write property test for animation state transitions
    - **Property 10: Animation State Transitions**
    - **Validates: Requirements 6.4**
    - Test that correct animations play for different input magnitudes
    - Run 100+ iterations with random input values

- [ ] 6. Checkpoint - Compile and verify basic functionality
  - Run `dotnet build "New Game Project Test Godot.sln"`
  - Verify no compilation errors
  - Fix any errors before proceeding
  - _Requirements: 9.1_

- [ ] 7. Remove obsolete code
  - [ ] 7.1 Delete BaseInputComponent.cs
    - Remove `addons/CoreComponents/BaseInputComponent.cs`
    - Verify no references remain in codebase
    - _Requirements: 7.1_
  
  - [ ] 7.2 Delete ComponentExtensions.cs
    - Remove `addons/CoreComponents/ComponentExtensions.cs`
    - Verify no references remain in codebase
    - _Requirements: 7.2_
  
  - [ ] 7.3 Clean up any remaining manual lookup code
    - Search for `FindAndSubscribeInput` calls
    - Search for manual `foreach` component lookup patterns
    - Remove any found instances
    - _Requirements: 7.3, 7.4_

- [ ] 8. Write comprehensive property tests
  - [ ] 8.1 Write property test for dependency injection completeness
    - **Property 1: Dependency Injection Completeness**
    - **Validates: Requirements 2.1, 2.3**
    - Test that entityInput is non-null in OnEntityReady()
    - Run 100+ iterations with random component configurations
  
  - [ ] 8.2 Write property test for event broadcasting
    - **Property 2: Event Broadcasting to Multiple Subscribers**
    - **Validates: Requirements 2.4**
    - Test that multiple components receive same event data
    - Run 100+ iterations with random subscriber counts
  
  - [ ] 8.3 Write property test for input direction mapping
    - **Property 3: Input Direction Mapping**
    - **Validates: Requirements 3.2**
    - Test that input keys produce correct direction vectors
    - Run 100+ iterations with random key combinations
  
  - [ ] 8.4 Write property test for event unsubscription cleanup
    - **Property 7: Event Unsubscription Cleanup**
    - **Validates: Requirements 4.6, 5.5, 6.6**
    - Test that components don't respond after _ExitTree()
    - Run 100+ iterations with random component lifecycles
  
  - [ ] 8.5 Write property test for backward compatibility
    - **Property 11: Backward Compatibility Preservation**
    - **Validates: Requirements 7.5**
    - Test that all behaviors match pre-refactoring baseline
    - Compare movement, jumping, rotation, animation states

- [ ] 9. Integration testing and verification
  - [ ] 9.1 Test player movement
    - Run game in Godot editor (F5)
    - Test WASD movement in all directions
    - Verify smooth movement and camera-relative direction
    - _Requirements: 9.2_
  
  - [ ] 9.2 Test player jumping
    - Test Space key jump
    - Verify jump only works when on ground
    - Verify jump velocity is correct
    - _Requirements: 9.3_
  
  - [ ] 9.3 Test character rotation
    - Verify character model faces movement direction
    - Verify smooth rotation interpolation
    - Test rotation with different movement directions
    - _Requirements: 9.4_
  
  - [ ] 9.4 Test character animations
    - Verify Idle animation plays when stationary
    - Verify Walk/Run animations play during movement
    - Verify Jump animations play when jumping
    - Verify smooth animation transitions
    - _Requirements: 9.5_
  
  - [ ] 9.5 Check for errors and warnings
    - Review Godot console output
    - Verify no dependency injection errors
    - Verify no null reference exceptions
    - Verify no memory leak warnings

- [ ] 10. Update documentation
  - [ ] 10.1 Update README.md
    - Replace BaseInputComponent references with IEntityInput
    - Update code examples to show interface implementation
    - Update dependency injection examples
    - _Requirements: 8.1_
  
  - [ ] 10.2 Update ARCHITECTURE.md
    - Document interface-based design pattern
    - Explain why interfaces work with Godot.Composition
    - Update architecture diagrams
    - _Requirements: 8.2_
  
  - [ ] 10.3 Update QUICK_START.md
    - Update quick start examples to use IEntityInput
    - Show how to implement custom input sources
    - Update component dependency examples
    - _Requirements: 8.3_
  
  - [ ] 10.4 Update MIGRATION_GUIDE.md
    - Add section on migrating from abstract class to interface
    - Document breaking changes
    - Provide step-by-step migration instructions
    - _Requirements: 8.4_
  
  - [ ] 10.5 Update all code examples
    - Search for BaseInputComponent in all documentation
    - Replace with IEntityInput
    - Update example code snippets
    - _Requirements: 8.5_
  
  - [ ] 10.6 Delete COMPONENT_EXTENSIONS_GUIDE.md
    - Remove guide for ComponentExtensions (no longer needed)
    - Update any references to this guide in other docs

- [ ] 11. Final checkpoint - Complete verification
  - Run full test suite (unit + property + integration)
  - Verify all tests pass
  - Run `dotnet build` and verify no warnings
  - Run game and complete manual testing checklist
  - Ensure all documentation is updated
  - Ask user if any questions or issues arise

## Notes

- All tasks are required for comprehensive refactoring with full test coverage
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation and catch errors early
- Property tests validate universal correctness properties across all inputs
- Integration tests verify end-to-end functionality matches pre-refactoring behavior
- Documentation updates ensure the new architecture is well-documented for future developers
