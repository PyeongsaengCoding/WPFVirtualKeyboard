using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WindowsInput.Native;
using WPFVirtualKeyboard.Core;
using WPFVirtualKeyboard.Helper;

namespace WPFVirtualKeyboard.Control
{
    public class VirtualKeyboard : UserControl
    {
        #region Variable

        private bool _keyPress;
        private Key _prevKey;

        #endregion

        #region Event

        #region VirtualKeyDown

        public static readonly RoutedEvent VirtualKeyDownEvent = EventManager.
            RegisterRoutedEvent("VirtualKeyDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualKeyboard));

        public event RoutedEventHandler VirtualKeyDown
        {
            add { AddHandler(VirtualKeyDownEvent, value); }
            remove { RemoveHandler(VirtualKeyDownEvent, value); }
        }

        #endregion

        #endregion

        #region Dependency Property

        #region IsShow

        public static readonly DependencyProperty IsShowProperty =
            DependencyProperty.Register
            ("IsShow", typeof(bool), typeof(VirtualKeyboard), new PropertyMetadata(true, ChangedIsShowProperty));

        public bool IsShow
        {
            get { return (bool)GetValue(IsShowProperty); }
            set { SetValue(IsShowProperty, value); }
        }

        private static void ChangedIsShowProperty(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var keyboard = obj as VirtualKeyboard;
            var isShow = (bool)e.NewValue;

            keyboard.ChangeShow(isShow);
        }

        #endregion

        #region UseGlobal

        public static readonly DependencyProperty UseGlobalProperty =
            DependencyProperty.Register
            ("UseGlobal", typeof(bool), typeof(VirtualKeyboard), new PropertyMetadata(true, ChangedUseGlobalProperty));

        public bool UseGlobal
        {
            get { return (bool)GetValue(UseGlobalProperty); }
            set { SetValue(UseGlobalProperty, value); }
        }

        private static void ChangedUseGlobalProperty(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Hook.UseGlobal = (bool)e.NewValue;
        }

        #endregion

        #region IsEnableHook

        public static readonly DependencyProperty IsEnableHookProperty =
            DependencyProperty.Register
            ("IsEnableHook", typeof(bool), typeof(VirtualKeyboard), new PropertyMetadata(false, ChangedIsEnableHookProperty));

        public bool IsEnableHook
        {
            get { return (bool)GetValue(IsEnableHookProperty); }
            set { SetValue(IsEnableHookProperty, value); }
        }

        private static void ChangedIsEnableHookProperty(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var keyboard = obj as VirtualKeyboard;

            if ((bool)e.NewValue)
            {
                keyboard.UpdateHookData();
                Hook.UseGlobal = keyboard.UseGlobal;

                Hook.MouseClickEvent += keyboard.Hook_MouseClickEvent;
                Hook.KeyClickEvent += keyboard.Hook_KeyClickEvent;
                Hook.Start();
            }
            else
            {
                Hook.MouseClickEvent -= keyboard.Hook_MouseClickEvent;
                Hook.KeyClickEvent -= keyboard.Hook_KeyClickEvent;
                Hook.Stop();
            }
        }

        #endregion

        #endregion

        #region Property

        public bool IsPressedShift { get; private set; }
        public bool IsPressedHangul { get; private set; }
        public bool IsPressedCapsLock { get; private set; }

        #endregion

        #region Constructor

        public VirtualKeyboard()
        {
            Loaded += KeyboardUserControl_Loaded;
            Unloaded += KeyboardUserControl_Unloaded;
            IsVisibleChanged += KeyboardUserControl_IsVisibleChanged;
        }

        #endregion

        #region Private Method

        private void UpdateHookData()
        {
            var content = Content as FrameworkElement;

            if (content == null)
            {
                return;
            }

            var area = VisualTreeHelper.GetDescendantBounds(content);

            if (!area.IsEmpty)
            {
                Hook.HookArea = area;
                Hook.HookElement = this;
            }
        }

        protected void UpdateKeys()
        {
            var content = Content as Panel;
            UpdateKeys(content);
        }

        protected void UpdateKeys(Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is Panel)
                {
                    var content = child as Panel;
                    UpdateKeys(content);
                }
                else if (child is Key)
                {
                    var keyButton = child as Key;
                    keyButton.UpdateKey(IsPressedShift, IsPressedCapsLock, IsPressedHangul);
                }
            }
        }

        private void ChangeShow(bool isShow)
        {
            var offset = VisualTreeHelper.GetOffset(this);

            Storyboard storyboard = new Storyboard();

            var to = isShow ? 0 : ActualHeight + offset.Y;

            DoubleAnimation doubleAnimation = new DoubleAnimation(to, new Duration(TimeSpan.FromSeconds(0.35)));
            doubleAnimation.EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut };

            storyboard.Children.Add(doubleAnimation);

            Storyboard.SetTargetProperty(storyboard, new PropertyPath("(0).(1)",
                new DependencyProperty[] { UIElement.RenderTransformProperty, TranslateTransform.YProperty }));

            BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace);
        }

        #endregion

        #region Event Handler

        private void Hook_MouseClickEvent(Win32Api.POINT point, Win32Api.MouseMessages msg)
        {
            var screenPoint = PointFromScreen(new Point(point.x, point.y));
            var result = TreeHelper.TryFindFromPoint<Key>(this, screenPoint);
            var key = result as Key;

            if (key == null)
            {
                return;
            }

            var keyCode = key.KeyCode;

            if (msg == Win32Api.MouseMessages.WM_LBUTTONDOWN)
            {
                _prevKey = key;
                key.IsPressed = true;

                if (key.ClickMode == ClickMode.Press)
                {
                    key.RaiseEvent(new RoutedEventArgs(Key.ClickEvent));
                }
            }
            else if (msg == Win32Api.MouseMessages.WM_LBUTTONUP)
            {
                if (keyCode != VirtualKeyCode.CAPITAL && keyCode != VirtualKeyCode.SHIFT)
                {
                    if (_prevKey != null)
                    {
                        _prevKey.IsPressed = false;
                        _prevKey = null;
                    }

                    key.IsPressed = false;
                }

                if (key.ClickMode == ClickMode.Release)
                {
                    key.RaiseEvent(new RoutedEventArgs(Key.ClickEvent));
                }
            }
        }

        private void Hook_KeyClickEvent(uint keyCode)
        {
            if (_keyPress)
            {
                return;
            }

            switch ((VirtualKeyCode)keyCode)
            {
                case VirtualKeyCode.HANGUL:
                    IsPressedHangul = !Simulator.Input.InputDeviceState.IsTogglingKeyInEffect(VirtualKeyCode.HANGUL);
                    break;

                case VirtualKeyCode.CAPITAL:
                    IsPressedCapsLock = !Simulator.Input.InputDeviceState.IsTogglingKeyInEffect(VirtualKeyCode.CAPITAL);
                    break;

                case VirtualKeyCode.LSHIFT:
                case VirtualKeyCode.RSHIFT:
                    IsPressedShift = !Simulator.Input.InputDeviceState.IsTogglingKeyInEffect(VirtualKeyCode.SHIFT);
                    break;
            }

            if (IsShow || Visibility == Visibility.Visible)
            {
                UpdateKeys();
            }
        }

        private void KeyClick(object sender, RoutedEventArgs e)
        {
            var key = e.OriginalSource as Key;

            if (key == null || (IsEnableHook && key.IsStylusCaptured))
            {
                return;
            }

            _keyPress = true;

            if (key.KeyCode == VirtualKeyCode.CAPITAL)
            {
                IsPressedCapsLock = !IsPressedCapsLock;
                Simulator.Keyboard.KeyPress(key.KeyCode);
            }
            else if (key.KeyCode == VirtualKeyCode.SHIFT)
            {
                IsPressedShift = !IsPressedShift;

                if (IsPressedShift)
                {
                    Simulator.Keyboard.KeyDown(key.KeyCode);
                }
                else
                {
                    Simulator.Keyboard.KeyUp(key.KeyCode);
                }
            }
            else if (key.KeyCode == VirtualKeyCode.HANGUL)
            {
                IsPressedHangul = !IsPressedHangul;
                Simulator.Keyboard.KeyPress(key.KeyCode);
            }
            else
            {
                if (key.KeyCode == VirtualKeyCode.RETURN)
                {
                    if (Keyboard.FocusedElement is TextBox)
                    {
                        var textBox = Keyboard.FocusedElement as TextBox;
                        var binding = textBox.GetBindingExpression(TextBox.TextProperty);

                        binding?.UpdateSource();
                    }
                }

                if (IsPressedShift)
                {
                    IsPressedShift = false;

                    Simulator.Keyboard.KeyPress(key.KeyCode);
                    Simulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
                }
                else
                {
                    Simulator.Keyboard.KeyPress(key.KeyCode);
                }
            }

            UpdateKeys();

            RaiseEvent(new RoutedEventArgs(VirtualKeyDownEvent, key.KeyCode));

            _keyPress = false;
        }

        private void KeyboardUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            RenderTransform = new TranslateTransform();
            AddHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);

            if (IsEnableHook)
            {
                UpdateHookData();

                //Hook.MouseClickEvent += Hook_MouseClickEvent; //마우스이벤트막기
                Hook.KeyClickEvent += Hook_KeyClickEvent;
                Hook.UseGlobal = UseGlobal;
                Hook.Start();
            }
            Send(System.Windows.Input.Key.HangulMode);
            Application.Current.MainWindow.Closed += (s, args) => Hook.Stop();
        }

        private void KeyboardUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Hook.MouseClickEvent -= Hook_MouseClickEvent;
            Hook.KeyClickEvent -= Hook_KeyClickEvent;
        }

        private void KeyboardUserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                if (IsEnableHook)
                {
                    IsPressedHangul = !Simulator.Input.InputDeviceState.IsTogglingKeyInEffect(VirtualKeyCode.HANGUL);
                    IsPressedCapsLock = Simulator.Input.InputDeviceState.IsTogglingKeyInEffect(VirtualKeyCode.CAPITAL);
                    UpdateHookData();
                }
                Send(System.Windows.Input.Key.HangulMode);
                UpdateKeys();
            }
        }

        #endregion

        public static void Send(System.Windows.Input.Key key)
        {
            if (Keyboard.PrimaryDevice != null)
            {
                if (Keyboard.PrimaryDevice.ActiveSource != null)
                {
                    var e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    InputManager.Current.ProcessInput(e);

                    // Note: Based on your requirements you may also need to fire events for:
                    // RoutedEvent = Keyboard.PreviewKeyDownEvent
                    // RoutedEvent = Keyboard.KeyUpEvent
                    // RoutedEvent = Keyboard.PreviewKeyUpEvent
                }
            }
        }

    }
}
