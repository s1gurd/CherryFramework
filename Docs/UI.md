# CherryFramework UI Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [View Service](#view-service)
4. [Presenter System](#presenter-system)
5. [Widget System](#widget-system)
6. [Populator System](#populator-system)
7. [UI Animation](#ui-animation)
8. [Performance Considerations](#performance-considerations)
9. [Common Issues and Solutions](#common-issues-and-solutions)
10. [Best Practices](#best-practices)
11. [Examples](#examples)
12. [Summary](#summary)

---

## Overview

The CherryFramework UI system provides a comprehensive, modular approach to building user interfaces in Unity. It implements a variant of the MVVM (Model-View-ViewModel) pattern with a focus on navigation, state management, and animation.

**Please see the Sample (`Assets/Sample/Scenes/dinoscene.unity`, GameObject `UIRoot`) for details how to setup the sytem! It is pretty easy and straightforward.**

### Key Features

- **View Service**: Centralized navigation and view stack management
- **Presenter System**: Screen-based UI with hierarchy support
- **Widget System**: Reusable, stateful UI components
- **Populator System**: Dynamic list/collection rendering with pooling
- **Animation System**: Declarative show/hide animations with sequencing
- **Modal/Popup Support**: Special handling for modal dialogs and popups
- **Dependency Injection**: Seamless integration with DI container
- **Data Binding**: Automatic UI updates via Accessor system

### UI Architecture Layers

| Layer            | Purpose                        | Components                                                    |
| ---------------- | ------------------------------ | ------------------------------------------------------------- |
| **View Service** | Navigation and view management | `ViewService`, `RootPresenterBase`                            |
| **Presenters**   | Screen-level UI containers     | `PresenterBase`, `PresenterErrorBase`, `PresenterLoadingBase` |
| **Widgets**      | Reusable UI components         | `WidgetBase`, `WidgetElement`, `WidgetState`                  |
| **Populators**   | Dynamic collection rendering   | `PopulatorBase<T>`, `PopulatorElementBase<T>`                 |
| **Animation**    | Visual transitions             | `UiAnimationBase`, various animators                          |

---

## Core Concepts

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         ViewService                             │
│  - Manages view stack                                           │
│  - Handles navigation (Pop/Back)                                │
│  - Tracks active view                                           │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌─────────────────────────┐       ┌─────────────────────────┐
│    RootPresenterBase    │       │    PresenterBase        │
│  - Root view container  │       │  - Screen UI            │
│  - Loading/Error screens│       │  - Child presenters     │
└─────────────────────────┘       │  - Show/Hide animations │
              │                   └─────────────────────────┘
              │                               │
              │                   ┌───────────┴───────────┐
              │                   ▼                       ▼
              │       ┌─────────────────┐     ┌─────────────────┐
              │       │  WidgetBase     │────▶│  WidgetElement  │
              │       │ - Multiple states     │ - Single element│
              │       │ - State machine │     │ - Show/Hide     │
              │       └─────────────────┘     └─────────────────┘
              │                   │                       │
              │                   └───────────┬───────────┘
              ▼                               ▼
┌─────────────────────────┐       ┌─────────────────────────┐
│    PopulatorBase<T>     │──────▶│  PopulatorElementBase<T>│
│  - Object pool          │       │  - Data container       │
│  - List rendering       │       │  - Refresh/Update       │
│  - Animation sequencing │       │  - Show/Hide animations │
└─────────────────────────┘       └─────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │  UiAnimationBase│
                    │ - Show()        │
                    │ - Hide()        │
                    │ - Sequencing    │
                    └─────────────────┘
```

### Key Components

| Component           | Purpose                                           |
| ------------------- | ------------------------------------------------- |
| `ViewService`       | Central navigation service managing view stack    |
| `RootPresenterBase` | Root container for all presenters                 |
| `PresenterBase`     | Base class for screen-level UI                    |
| `WidgetBase`        | Stateful UI component with multiple visual states |
| `WidgetElement`     | Individual UI element with show/hide              |
| `PopulatorBase<T>`  | Dynamic list renderer with pooling                |
| `UiAnimationBase`   | Base class for all UI animations                  |

---

## View Service

**Namespace**: `CherryFramework.UI.Views`

**Purpose**: Central navigation service that manages the view stack, handles presenter transitions, and tracks the active view.

### Class Definition

```csharp
public class ViewService : GeneralClassBase
{
    public delegate void OnViewChangedDelegate();
    public event OnViewChangedDelegate OnAnyViewBecameActive;
    public event OnViewChangedDelegate OnAllViewsBecameInactive;

    public bool IsViewActive { get; }
    public bool IsLastView { get; }
    public PresenterBase ActiveView { get; private set; }

    public ViewService(RootPresenterBase root, bool debugMessages);

    // Pop views by type
    public Sequence PopView<T>(PresenterBase mountingPoint = null, bool skipAnimation = false) where T : PresenterBase;
    public Sequence PopView<T>(out T newView, PresenterBase mountingPoint = null, bool skipAnimation = false);
    public Sequence PopView(Type type, PresenterBase mountingPoint = null, bool skipAnimation = false);

    // Pop views by instance
    public Sequence PopView(PresenterBase view, PresenterBase mountingPoint = null, bool skipAnimation = false);

    // Navigation
    public Sequence Back(bool skipAnimation = false);
    public Sequence HideAndReset(bool skipAnimation = false);
    public void ClearHistory();

    // Special screens
    public Sequence PopLoadingView();
    public Sequence PopErrorView(string title, string message);
}
```

### Constructor

```csharp
public ViewService(RootPresenterBase root, bool debugMessages)
```

**Example**:

```csharp
// In installer
var rootPresenter = FindObjectOfType<RootPresenterBase>();
var viewService = new ViewService(rootPresenter, debugMessages: true);
DependencyContainer.Instance.BindAsSingleton(viewService);
```

### Navigation Methods

#### PopView (by type)

```csharp
public Sequence PopView<T>(PresenterBase mountingPoint = null, bool skipAnimation = false) where T : PresenterBase
```

**Example**:

```csharp
// Show settings screen
_viewService.PopView<SettingsPresenter>();

// Show inventory as child of HUD
_viewService.PopView<InventoryPresenter>(hudPresenter);
```

#### PopView with output

```csharp
public Sequence PopView<T>(out T newView, PresenterBase mountingPoint = null, bool skipAnimation = false)
```

**Example**:

```csharp
_viewService.PopView<SettingsPresenter>(out var settingsView);
settingsView.SetConfiguration(currentSettings);
```

#### Back navigation

```csharp
public Sequence Back(bool skipAnimation = false)
```

**Example**:

```csharp
// Go back to previous screen
_viewService.Back();

// Go back without animation
_viewService.Back(skipAnimation: true);
```

#### HideAndReset

```csharp
public Sequence HideAndReset(bool skipAnimation = false)
```

**Example**:

```csharp
// Hide all views and reset to empty state
_viewService.HideAndReset();
```

### Event Handling

```csharp
public class GameUI : MonoBehaviour
{
    [Inject] private ViewService _viewService;

    private void Start()
    {
        _viewService.OnAnyViewBecameActive += OnViewOpened;
        _viewService.OnAllViewsBecameInactive += OnAllViewsClosed;
    }

    private void OnViewOpened()
    {
        Debug.Log($"View opened: {_viewService.ActiveView?.name}");
    }

    private void OnAllViewsClosed()
    {
        Debug.Log("All views closed - showing main menu?");
    }
}
```

---

## Presenter System

### PresenterBase (Abstract)

**Namespace**: `CherryFramework.UI.InteractiveElements.Presenters`

**Purpose**: Base class for all screen-level UI components. Manages child presenters, animations, and view hierarchy. All child presenters must added to childPresenters manually in the Editor

```csharp
public abstract class PresenterBase : InteractiveElementBase
{
    [Inject] protected ViewService ViewService;

    [Header("Hierarchy settings")]
    [SerializeField] private Canvas childrenContainer;
    [SerializeField] protected List<PresenterBase> childPresenters = new();

    public Canvas ChildrenContainer => childrenContainer;
    public List<PresenterBase> ChildPresenters => childPresenters;
    public virtual bool Modal { get; private set; }

    public List<PresenterBase> uiPath { get; set; } = new();
    public PresenterBase currentChild { get; set; }

    public void InitializePresenter();
    public virtual Sequence ShowFrom(PresenterBase previous, bool skipAnimation = false);
    public virtual Sequence HideTo(PresenterBase next, bool skipAnimation = false);
}
```

**Example**:

```csharp
public class MainMenuPresenter : PresenterBase
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    protected override void OnPresenterInitialized()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        _quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        ViewService.PopView<GameplayPresenter>();
    }

    private void OnSettingsClicked()
    {
        ViewService.PopView<SettingsPresenter>();
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }

    public override Sequence ShowFrom(PresenterBase previous, bool skipAnimation = false)
    {
        Debug.Log($"Showing main menu, previous: {previous?.name}");
        return base.ShowFrom(previous, skipAnimation);
    }
}
```

### RootPresenterBase

**Namespace**: `CherryFramework.UI.Views`

**Purpose**: Root container that holds all presenters and provides access to special screens. All child presenters must added to childPresenters manually in the Editor

```csharp
public class RootPresenterBase : PresenterBase
{
    [SerializeField] private PresenterLoadingBase loadingScreen;
    [SerializeField] private PresenterErrorBase errorScreen;

    public PresenterLoadingBase LoadingScreen => loadingScreen;
    public PresenterErrorBase ErrorScreen => errorScreen;
}
```

**Example**:

```csharp
// In your scene hierarchy:
// Canvas (RootPresenterBase)
// ├── LoadingScreen (PresenterLoadingBase)
// ├── ErrorScreen (PresenterErrorBase)
// ├── Menus
// │   ├── MainMenu (PresenterBase)
// │   └── Settings (PresenterBase)
// └── Gameplay
//     ├── HUD (PresenterBase)
//     └── PauseMenu (PresenterBase)
```

### PresenterErrorBase

**Namespace**: `CherryFramework.UI.InteractiveElements.Presenters`

**Purpose**: Specialized presenter for error screens with predefined UI elements.

```csharp
public abstract class PresenterErrorBase : PresenterBase, IPopUp
{
    [SerializeField] private TMP_Text errorTitle;
    [SerializeField] private TMP_Text errorMsg;
    [SerializeField] private Button backButton;

    public void SetError(string title, string message)
    {
        errorTitle.text = title;
        errorMsg.text = message;
    }
}
```

**Example**:

```csharp
// Show error screen
_viewService.PopErrorView("Connection Failed", "Please check your internet connection.");
```

### PresenterLoadingBase

**Namespace**: `CherryFramework.UI.InteractiveElements.Presenters`

**Purpose**: Specialized presenter for loading screens.

```csharp
public abstract class PresenterLoadingBase : PresenterBase, IPopUp
{
    // Can be extended with progress bars, tips, etc.
}
```

**Example**:

```csharp
// Show loading screen while async operation completes
_viewService.PopLoadingView();

// Later, when loading completes
_viewService.Back();
```

---

## Widget System

### WidgetBase

**Namespace**: `CherryFramework.UI.InteractiveElements.Widgets`

**Purpose**: Stateful UI component that can switch between multiple visual states with smooth transitions.

```csharp
public class WidgetBase : InteractiveElementBase
{
    [SerializeField] private WidgetStartupBehaviour startupBehaviour;
    [SerializeField] protected List<WidgetState> widgetStates = new();

    public int CurrentState { get; private set; }
    public int StatesCount => widgetStates.Count;
    public bool Playing { get; private set; }

    public event OnStateChangedDelegate OnStartStateChange;
    public event OnStateChangedDelegate OnFinishStateChange;

    public void SetState(int state);
    public void SetState(string stateName);
    public string GetStateName(int state);
}
```

**Example**:

```csharp
[Serializable]
public class ButtonState
{
    public Sprite backgroundSprite;
    public Color textColor;
    public AudioClip clickSound;
}

public class StatefulButton : WidgetBase
{
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Button _button;

    [SerializeField] private List<ButtonState> _buttonStates;

    protected override void OnEnable()
    {
        base.OnEnable();
        _button.onClick.AddListener(OnClick);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _button.onClick.RemoveListener(OnClick);
    }

    public void SetState(int stateIndex)
    {
        SetState(stateIndex);
        // WidgetBase will handle the state transition
    }

    protected override void OnShowComplete()
    {
        base.OnShowComplete();
        ApplyCurrentState();
    }

    private void ApplyCurrentState()
    {
        var state = _buttonStates[CurrentState];
        _background.sprite = state.backgroundSprite;
        _label.color = state.textColor;
    }

    private void OnClick()
    {
        var stateName = GetStateName(CurrentState);
        Debug.Log($"Button clicked in state: {stateName}");
    }
}
```

### WidgetState

**Namespace**: `CherryFramework.UI.InteractiveElements.Widgets`

**Purpose**: Defines a single state for a widget, containing the UI elements that should be active in that state.

```csharp
[Serializable]
public class WidgetState
{
    public string stateName = "";
    public List<WidgetElement> stateElements = new();
}
```

**Example**:

```csharp
// In Unity Inspector:
// WidgetState (Normal)
//   - NormalIcon (WidgetElement)
//   - NormalLabel (WidgetElement)
//
// WidgetState (Hover)
//   - HoverIcon (WidgetElement)
//   - HoverLabel (WidgetElement)
//
// WidgetState (Pressed)
//   - PressedIcon (WidgetElement)
//   - PressedLabel (WidgetElement)
```

### WidgetStartupBehaviour

**Namespace**: `CherryFramework.UI.InteractiveElements.Widgets`

**Purpose**: Determines how the widget initializes its states.

| Value                                            | Description                            |
| ------------------------------------------------ | -------------------------------------- |
| `ExecuteShowOnCurrentState`                      | Only show the current state            |
| `SimultaneouslyExecuteShowOnSelfAndCurrentState` | Show widget and current state together |
| `SequentiallyExecuteShowOnSelfAndCurrentState`   | Show widget, then current state        |
| `JustSetCurrentState`                            | Set state without animations           |

---

### WidgetElement

**Namespace**: `CherryFramework.UI.InteractiveElements.Widgets`

**Purpose**: Individual UI element that can be shown/hidden, typically used as part of a widget state.

```csharp
public class WidgetElement : InteractiveElementBase
{
    public virtual Sequence Show()
    {
        return CreateSequence(animators, Purpose.Show);
    }

    public virtual Sequence Hide()
    {
        return CreateSequence(animators, Purpose.Hide);
    }
}
```

**Example**:

```csharp
public class AnimatedIcon : WidgetElement
{
    [SerializeField] private Image _icon;
    [SerializeField] private float _pulseAmount = 1.2f;

    public void SetIcon(Sprite sprite)
    {
        _icon.sprite = sprite;
    }

    public Sequence Pulse()
    {
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(_pulseAmount, 0.2f));
        seq.Append(transform.DOScale(1f, 0.2f));
        return seq;
    }
}
```

---

## Populator System

### PopulatorBase<T>

**Namespace**: `CherryFramework.UI.InteractiveElements.Populators`

**Purpose**: Dynamically renders collections of data using pooled UI elements.

```csharp
public abstract class PopulatorBase<T> where T : class
{
    protected T[] Data;
    protected PopulatorElementBase<T> ElementSample;
    protected Transform ElementsRoot;

    public IReadOnlyCollection<PopulatorElementBase<T>> Active => active;

    protected PopulatorBase(PopulatorElementBase<T> elementSample, Transform root);

    public virtual void UpdateElements(IEnumerable<T> data, float delayEveryElement = 0f);
    public void Clear();
}
```

**Example**:

```csharp
public class InventoryPopulator : PopulatorBase<ItemData>
{
    public InventoryPopulator(InventoryItemElement sample, Transform root) 
        : base(sample, root)
    {
    }

    public override void UpdateElements(IEnumerable<ItemData> items, float delayEveryElement = 0f)
    {
        base.UpdateElements(items, delayEveryElement);

        // Additional logic after population
        Debug.Log($"Inventory updated with {Data.Length} items");
    }
}

// Usage
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventoryItemElement _itemPrefab;
    [SerializeField] private Transform _contentRoot;

    private InventoryPopulator _populator;

    private void Awake()
    {
        _populator = new InventoryPopulator(_itemPrefab, _contentRoot);
    }

    public void ShowInventory(List<ItemData> items)
    {
        _populator.UpdateElements(items, 0.05f); // Staggered animation
    }
}
```

### PopulatorElementBase<T>

**Namespace**: `CherryFramework.UI.InteractiveElements.Populators`

**Purpose**: Base class for elements that are populated by a Populator.

```csharp
public abstract class PopulatorElementBase<T> : WidgetElement where T : class
{
    public T data;

    public virtual void SetData(T data)
    {
        this.data = data;
    }

    public virtual Sequence Refresh()
    {
        var seq = CreateSequence(animators, Purpose.Hide);
        seq.Append(CreateSequence(animators, Purpose.Show));
        seq.AppendCallback(OnRefreshComplete);
        return seq;
    }

    protected virtual void OnRefreshComplete() { }
}
```

**Example**:

```csharp
public class InventoryItemElement : PopulatorElementBase<ItemData>
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Image _iconImage;

    public override void SetData(ItemData data)
    {
        base.SetData(data);

        _nameText.text = data.itemName;
        _countText.text = $"x{data.count}";
        _iconImage.sprite = data.icon;
    }

    public override Sequence Refresh()
    {
        // Custom refresh animation
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(0.8f, 0.1f));
        seq.Append(transform.DOScale(1f, 0.2f));
        seq.AppendCallback(() => Debug.Log($"Refreshed {data.itemName}"));
        return seq;
    }
}
```

---

## UI Animation

### UiAnimationBase (Abstract)

**Namespace**: `CherryFramework.UI.UiAnimation`

**Purpose**: Base class for all UI animations.

```csharp
public abstract class UiAnimationBase : MonoBehaviour
{
    [SerializeField] protected float duration = 0.3f;
    [SerializeField] protected Ease showEasing = Ease.OutQuad;
    [SerializeField] protected Ease hideEasing = Ease.OutQuad;

    protected RectTransform Target { get; }
    protected Sequence MainSequence;

    public void Initialize();
    public abstract Sequence Show(float delay = 0f);
    public abstract Sequence Hide(float delay = 0f);
}
```

### UiAnimationSettings

**Namespace**: `CherryFramework.UI.UiAnimation`

**Purpose**: Configures an animator with delay and launch mode.

```csharp
[Serializable]
public class UiAnimationSettings
{
    public UiAnimationBase animator;
    public float delay = 0f;
    public LaunchMode launchMode;
}
```

### LaunchMode

**Namespace**: `CherryFramework.UI.UiAnimation.Enums`

**Purpose**: Determines when an animation plays in a sequence.

| Value                           | Description                                  |
| ------------------------------- | -------------------------------------------- |
| `AtGlobalAnimationStart`        | Starts at the beginning of the sequence      |
| `AtPreviousAnimatorStart`       | Starts at the same time as previous animator |
| `AfterPreviousAnimatorFinished` | Starts after previous animator completes     |

### Built-in Animators

#### UiFade

```csharp
[RequireComponent(typeof(CanvasGroup))]
public class UiFade : UiAnimationBase
{
    // Fades the CanvasGroup alpha
}

// Usage in inspector:
// Add to any UI element with CanvasGroup
```

#### UiScale

```csharp
[RequireComponent(typeof(RectTransform))]
public class UiScale : UiAnimationBase
{
    [SerializeField] private UiAnimatorEndValueTypes type;
    [SerializeField] private Vector3 value;
}

// Scales the RectTransform
```

#### UiSlide

```csharp
[RequireComponent(typeof(RectTransform))]
public class UiSlide : UiAnimationBase
{
    [SerializeField] private Vector2 positionDelta;
    [SerializeField] private bool reverseDirectionOnHide = true;
}

// Slides based on percentage of element size
```

#### UiTextFade

```csharp
[RequireComponent(typeof(TMP_Text))]
public class UiTextFade : UiAnimationBase
{
    // Fades TMP_Text alpha
}
```

#### UiActive

```csharp
[RequireComponent(typeof(RectTransform))]
public class UiActive : UiAnimationBase
{
    // Simply sets gameObject active/inactive
}
```

### Animation Example

```csharp
public class AnimatedPanel : InteractiveElementBase
{
    [SerializeField] private List<UiAnimationSettings> _customAnimations; // Fill in the Editor

    protected override void OnShowStart()
    {
        base.OnShowStart();
        Debug.Log("Panel show started");
    }

    protected override void OnShowComplete()
    {
        base.OnShowComplete();
        Debug.Log("Panel show completed");
    }

    public void PlayCustomSequence()
    {
        var seq = CreateSequence(_customAnimations, Purpose.Show);
        seq.Play();
    }
}
```

---

## Performance Considerations

### 1. View Stack Size

```csharp
// GOOD - Reasonable stack depth
_viewService.PopView<MenuPresenter>();
_viewService.PopView<SettingsPresenter>();
_viewService.Back(); // Back to menu

// BAD - Deep stacks can use memory
for (int i = 0; i < 50; i++)
{
    _viewService.PopView<DeepPresenter>(); // Don't do this!
}
```

### 2. Populator Pool Size

```csharp
public class OptimizedPopulator<T> : PopulatorBase<T> where T : class
{
    private int _maxPoolSize = 50;

    public OptimizedPopulator(PopulatorElementBase<T> sample, Transform root, int maxSize) 
        : base(sample, root)
    {
        _maxPoolSize = maxSize;
    }

    public override void UpdateElements(IEnumerable<T> data, float delayEveryElement = 0f)
    {
        var dataArray = data as T[] ?? data.ToArray();

        // Limit data size
        if (dataArray.Length > _maxPoolSize)
        {
            Debug.LogWarning($"Truncating data from {dataArray.Length} to {_maxPoolSize}");
            dataArray = dataArray.Take(_maxPoolSize).ToArray();
        }

        base.UpdateElements(dataArray, delayEveryElement);
    }
}
```

### 3. Animation Overhead

```csharp
// GOOD - Batch animations
public Sequence ShowAll()
{
    var seq = DOTween.Sequence();
    foreach (var element in _elements)
    {
        seq.Join(element.Show()); // All play together
    }
    return seq;
}

// BAD - Simultaneous animations for many elements
public Sequence ShowAllSequential()
{
    var seq = DOTween.Sequence();
    foreach (var element in _elements)
    {
        seq.Insert(0f, element.Show()); // Plays one after another - slow!
    }
    return seq;
}
```

### 4. Widget State Complexity

```csharp
// GOOD - Simple states
public class SimpleButton : WidgetBase
{
    [SerializeField] private List<WidgetState> _states; // 2-3 states
}

// BAD - Too many states, consider using nested widgets
public class ComplexWidget : WidgetBase
{
    [SerializeField] private List<WidgetState> _states; // 20+ states - hard to manage
}
```

---

## Common Issues and Solutions

### Issue 1: Presenter Not Showing

**Symptoms**: `PopView()` called but nothing appears, or errors are displayed in Console

**Solutions**:

```csharp
// SOLUTION 1: Ensure presenter is registered
public class MyInstaller : InstallerBehaviourBase{
    [SerializeField] private RootPresenterBase _root;

    protected override void Install()
    {
        // Make sure all child presenters are assigned in inspector
        Debug.Log($"Root has {_root.ChildPresenters.Count} child presenters");
    }
}

// SOLUTION 2: Check if view exists in container
public bool CanShowPresenter<T>() where T : PresenterBase
{
    var root = FindObjectOfType<RootPresenter>();
    return root.ChildPresenters.Any(p => p is T);
}

```

### Issue 2: Modal Blocks Navigation

**Symptoms**: `Back()` doesn't work, can't navigate past certain screens

**Solution**:

```csharp
public class ModalPresenter : PresenterBase
{
    [SerializeField] private bool _isModal = true;

    public override bool Modal => _isModal;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Handle modal-specific back behavior
            ViewService.Back();
        }
    }
}

// ViewService automatically blocks navigation past modal
// _history.TryPeek(out var current)
// if (current.Last() is IModal || current.Last().Modal)
// {
//     return; // Navigation blocked
// }
```

---

## Best Practices

### 1. Organize Presenters Hierarchically

```csharp
// GOOD - Clear hierarchy
// RootPresenter
// ├── MainMenuPresenter
// │   └── SettingsPresenter (child of MainMenu)
// ├── GameplayPresenter
// │   ├── HUDPresenter
// │   └── PausePresenter (modal)
// └── LoadingPresenter

// In code
public class GameplayPresenter : PresenterBase
{
    [SerializeField] private HUDPresenter _hud;
    [SerializeField] private PausePresenter _pause;

    protected override void OnPresenterInitialized()
    {
        childPresenters.Add(_hud);
        childPresenters.Add(_pause);
    }
}
```

### 2. Use Widgets for Reusable Components

```csharp
// Create reusable widgets
public class HealthBarWidget : WidgetBase
{
    [SaveGameData] private float _health;

    public void SetHealth(float health)
    {
        _health = health;
        UpdateState();
    }

    private void UpdateState()
    {
        if (_health > 50) SetState("Healthy");
        else if (_health > 20) SetState("Warning");
        else SetState("Critical");
    }
}

// Reuse everywhere
public class PlayerHUD : PresenterBase
{
    [SerializeField] private HealthBarWidget _playerHealth;
    [SerializeField] private HealthBarWidget _bossHealth;
}
```

### 3. Use Populators for Dynamic Lists

```csharp
public class ShopUI : PresenterBase
{
    [SerializeField] private ShopItemElement _itemPrefab;
    [SerializeField] private Transform _itemContainer;

    private PopulatorBase<ShopItem> _populator;

    protected override void OnPresenterInitialized()
    {
        base.OnPresenterInitialized();
        _populator = new PopulatorBase<ShopItem>(_itemPrefab, _itemContainer);
    }

    public void ShowItems(List<ShopItem> items)
    {
        _populator.UpdateElements(items, 0.03f); // Staggered appearance
    }
}
```

### 4. Leverage Animation Sequencing

```csharp
public class OnboardingFlow : PresenterBase
{
    [SerializeField] private List<InteractiveElementBase> _steps;

    public Sequence PlayOnboarding()
    {
        var seq = DOTween.Sequence();

        foreach (var step in _steps)
        {
            seq.Append(step.Show());
            seq.AppendInterval(2f);
            seq.Append(step.Hide());
        }

        seq.OnComplete(() => {
            ViewService.Back(); // Return to previous screen
        });

        return seq;
    }
}
```

### 5. Handle Modal Views Properly

```csharp
public class ConfirmationDialog : PresenterBase, IModal
{
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private System.Action _onConfirm;
    private System.Action _onCancel;

    public void Show(string message, System.Action onConfirm, System.Action onCancel = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        // Set message text
        ViewService.PopView(this);
    }

    public void OnConfirm()
    {
        _onConfirm?.Invoke();
        ViewService.Back();
    }

    public void OnCancel()
    {
        _onCancel?.Invoke();
        ViewService.Back();
    }
}
```

### 6. Use ViewService Events for Global UI Logic

```csharp
public class UISoundManager : MonoBehaviour
{
    [Inject] private ViewService _viewService;
    [Inject] private SoundService _sound;

    private void Start()
    {
        _viewService.OnAnyViewBecameActive += OnViewChanged;
    }

    private void OnViewChanged()
    {
        var view = _viewService.ActiveView;

        if (view is SettingsPresenter)
            _sound.Play("ui_settings_open");
        else if (view is InventoryPresenter)
            _sound.Play("ui_inventory_open");
    }
}
```

### 7. Implement Loading States

```csharp
public class AsyncOperationPresenter : PresenterBase
{
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _statusText;

    public async void LoadAsync()
    {
        ViewService.PopLoadingView();

        _statusText.text = "Loading...";

        var operation = SomeAsyncOperation();

        while (!operation.IsCompleted)
        {
            _progressBar.value = operation.Progress;
            await Task.Delay(100);
        }

        _statusText.text = "Complete!";
        await Task.Delay(500);

        ViewService.Back();
    }
}
```

### 8. Debug View Hierarchy

```csharp
public static class ViewDebugger
{
    public static void LogViewHierarchy(ViewService viewService)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("--- View Hierarchy ---");

        var root = DependencyContainer.Instance.GetInstance<RootPresenterBase>();
        LogPresenter(sb, root, 0);

        Debug.Log(sb.ToString());
    }

    private static void LogPresenter(System.Text.StringBuilder sb, PresenterBase presenter, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}├─ {presenter.name} ({presenter.GetType().Name})");

        foreach (var child in presenter.ChildPresenters)
        {
            LogPresenter(sb, child, depth + 1);
        }
    }
}
```

---

## Examples

### Complete UI Flow Example

```csharp
// 1. Define presenters
public class MainMenuPresenter : PresenterBase
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    protected override void OnPresenterInitialized()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        _quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        ViewService.PopView<GameplayPresenter>();
    }

    private void OnSettingsClicked()
    {
        ViewService.PopView<SettingsPresenter>(this);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }
}

public class SettingsPresenter : PresenterBase
{
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Button _backButton;

    [Inject] private SettingsModel _settings;

    protected override void OnPresenterInitialized()
    {
        _volumeSlider.value = _settings.Volume;
        _fullscreenToggle.isOn = _settings.Fullscreen;

        _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        _backButton.onClick.AddListener(() => ViewService.Back());
    }

    private void OnVolumeChanged(float volume)
    {
        _settings.Volume = volume;
    }

    private void OnFullscreenChanged(bool fullscreen)
    {
        _settings.Fullscreen = fullscreen;
    }
}

public class GameplayPresenter : PresenterBase
{
    [SerializeField] private HUDPresenter _hud;
    [SerializeField] private PauseMenuPresenter _pauseMenu;

    private bool _isPaused;

    protected override void OnPresenterInitialized()
    {
        childPresenters.Add(_hud);
        childPresenters.Add(_pauseMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;

        if (_isPaused)
        {
            ViewService.PopView<PauseMenuPresenter>(this);
            Time.timeScale = 0f;
        }
        else
        {
            ViewService.Back();
            Time.timeScale = 1f;
        }
    }
}

// 2. Widget for HUD
public class HUDPresenter : PresenterBase
{
    [SerializeField] private HealthBarWidget _healthBar;
    [SerializeField] private ScoreWidget _scoreWidget;
    [SerializeField] private AmmoWidget _ammoWidget;

    [Inject] private PlayerModel _player;

    protected override void OnPresenterInitialized()
    {
        // Bind to model
        _player.HealthAccessor.BindDownwards(health => _healthBar.SetHealth(health));
        _player.ScoreAccessor.BindDownwards(score => _scoreWidget.SetScore(score));
        _player.AmmoAccessor.BindDownwards(ammo => _ammoWidget.SetAmmo(ammo));
    }
}

public class HealthBarWidget : WidgetBase
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _text;

    public void SetHealth(float health)
    {
        _slider.value = health / 100f;
        _text.text = $"{health:F0}%";

        if (health < 20) SetState("Critical");
        else if (health < 50) SetState("Warning");
        else SetState("Healthy");
    }
}

// 3. Populator for inventory
public class InventoryPresenter : PresenterBase
{
    [SerializeField] private InventoryItemElement _itemPrefab;
    [SerializeField] private Transform _contentRoot;

    private PopulatorBase<ItemData> _populator;

    protected override void OnPresenterInitialized()
    {
        _populator = new PopulatorBase<ItemData>(_itemPrefab, _contentRoot);
    }

    public void ShowInventory(List<ItemData> items)
    {
        _populator.UpdateElements(items, 0.03f);
    }
}

public class InventoryItemElement : PopulatorElementBase<ItemData>
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Image _iconImage;

    public override void SetData(ItemData data)
    {
        base.SetData(data);

        _nameText.text = data.itemName;
        _countText.text = $"x{data.count}";
        _iconImage.sprite = data.icon;
    }
}

// 4. Installer
[DefaultExecutionOrder(-10000)]
public class UIInstaller : InstallerBehaviourBase
{
    [SerializeField] private RootPresenterBase _rootPresenter;
    [SerializeField] private bool _debugViews = true;

    protected override void Install()
    {
        var viewService = new ViewService(_rootPresenter, _debugViews);
        BindAsSingleton(viewService);
    }
}

// 5. Usage in game
public class GameController : MonoBehaviour
{
    [Inject] private ViewService _viewService;
    [Inject] private InventoryPresenter _inventory;

    private void Start()
    {
        // Start with main menu
        _viewService.PopView<MainMenuPresenter>();
    }

    public void OpenInventory(List<ItemData> items)
    {
        _viewService.PopView<InventoryPresenter>(out var inventory);
        inventory.ShowInventory(items);
    }
}
```

---

## Summary

### Architecture Diagram Recap

```
┌─────────────────────────────────────────────────────────────────┐
│                         ViewService                             │
│                         (Navigation)                            │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌─────────────────────────┐       ┌─────────────────────────┐
│    RootPresenterBase    │──────▶│    PresenterBase        │
│    (Container)          │       │    (Screen)             │
└─────────────────────────┘       └─────────────────────────┘
                                            │
                            ┌───────────────┴───────────────┐
                            ▼                               ▼
                ┌─────────────────┐              ┌─────────────────┐
                │   WidgetBase    │              │  PopulatorBase  │
                │   (Stateful)    │              │   (Dynamic)     │
                └─────────────────┘              └─────────────────┘
                        │                                  │
                        ▼                                  ▼
                ┌─────────────────┐              ┌─────────────────┐
                │  WidgetElement  │              │PopulatorElement │
                │   (Element)     │              │    (Element)    │
                └─────────────────┘              └─────────────────┘
                        │                                  │
                        └───────────────┬──────────────────┘
                                        ▼
                               ┌─────────────────┐
                               │ UiAnimationBase │
                               │  (Animation)    │
                               └─────────────────┘
```

### Key Components Summary

| Component                 | Purpose            | Key Methods                                       |
| ------------------------- | ------------------ | ------------------------------------------------- |
| `ViewService`             | Navigation         | `PopView<T>()`, `Back()`, `ClearHistory()`        |
| `PresenterBase`           | Screen UI          | `ShowFrom()`, `HideTo()`, `InitializePresenter()` |
| `WidgetBase`              | Stateful component | `SetState()`, state change events                 |
| `WidgetElement`           | UI element         | `Show()`, `Hide()`                                |
| `PopulatorBase<T>`        | Dynamic lists      | `UpdateElements()`, `Clear()`                     |
| `PopulatorElementBase<T>` | List item          | `SetData()`, `Refresh()`                          |
| `UiAnimationBase`         | Animation          | `Show()`, `Hide()`                                |

### Key Points

| #   | Key Point                                     | Why It Matters                                        |
| --- | --------------------------------------------- | ----------------------------------------------------- |
| 1   | **ViewService manages a stack of presenters** | Enables back navigation and view history              |
| 2   | **Presenters can have child presenters**      | Creates hierarchical UI structures                    |
| 3   | **Modal presenters block back navigation**    | Ensures modal dialogs behave correctly                |
| 4   | **Widgets maintain multiple visual states**   | Perfect for tabs, buttons, toggles, status indicators |
| 5   | **Populators use object pooling**             | Efficient rendering of large lists                    |
| 6   | **Animations are declarative and sequenced**  | Complex transitions with minimal code                 |
| 7   | **All UI components are injectable**          | Seamless integration with DI container                |
| 8   | **Loading and error screens are built-in**    | Consistent user experience                            |

### When to Use Each Component

| Component          | Use When                                                  |
| ------------------ | --------------------------------------------------------- |
| `PresenterBase`    | Creating a new screen/menu                                |
| `WidgetBase`       | Building reusable components with multiple states         |
| `WidgetElement`    | Creating individual UI pieces |
| `PopulatorBase<T>` | Displaying dynamic lists (inventory, shop, leaderboards)  |
| Custom Animator    | Creating unique transition effects                        |

The CherryFramework UI system provides a robust, scalable foundation for building complex user interfaces with clean separation of concerns, efficient rendering, and smooth animations.


