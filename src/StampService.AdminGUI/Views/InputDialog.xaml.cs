using System.Windows;

namespace StampService.AdminGUI.Views;

public partial class InputDialog : Window
{
 public string ResponseText { get; private set; } = string.Empty;

    public InputDialog(string question, string title)
    {
        InitializeComponent();
  Title = title;
    QuestionText.Text = question;
    ResponseTextBox.Focus();
 }

private void OkButton_Click(object sender, RoutedEventArgs e)
    {
 ResponseText = ResponseTextBox.Text;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
  DialogResult = false;
    }
}
