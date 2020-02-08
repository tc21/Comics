using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Comics.UI {

    public partial class ChangeTrackingTextBox : UserControl {
        public ChangeTrackingTextBox() {
            this.InitializeComponent();
            this.RootElement.DataContext = this;
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            this.SaveState();
        }

        public string Label {
            get => this.GetValue(LabelProperty) as string;
            set => this.SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ChangeTrackingTextBox), new PropertyMetadata(""));

        public string Value {
            get => this.GetValue(ValueProperty) as string;
            set => this.SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(ChangeTrackingTextBox), new PropertyMetadata(""));


        public Brush InactiveForeground {
            get => this.GetValue(InactiveForegroundProperty) as Brush;
            set => this.SetValue(InactiveForegroundProperty, value);
        }

        public static readonly DependencyProperty InactiveForegroundProperty =
            DependencyProperty.Register("InactiveForeground", typeof(Brush), typeof(ChangeTrackingTextBox), new PropertyMetadata(InactiveTextForeground));

        private static readonly Brush InactiveTextForeground = new SolidColorBrush(Color.FromRgb(0x7f, 0x7f, 0x7f));
        private string savedValue;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if ((sender as TextBox).IsFocused) {
                this.Changed = true;
                this.TextBox.Foreground = this.Foreground;
                this.ChangedIndicator.Visibility = Visibility.Visible;
            }
        }

        public void SaveState() {
            this.savedValue = this.Value;
            this.Initialize();
        }

        public void RevertChanges() {
            this.Value = this.savedValue;
            this.Initialize();
        }

        private void Initialize() {
            this.Changed = false;
            this.TextBox.Foreground = this.InactiveForeground;
            this.ChangedIndicator.Visibility = Visibility.Hidden;
        }

        public bool Changed { get; private set; } = false;
    }
}
