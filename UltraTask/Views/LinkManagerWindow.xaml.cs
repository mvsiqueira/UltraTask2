using System.Windows;
using System.Windows.Controls;
using UltraTask.Models;

namespace UltraTask.Views;

public partial class LinkManagerWindow : Window
{
    private readonly List<LinkRule> _rules;
    private readonly Action _onChanged;

    public LinkManagerWindow(List<LinkRule> rules, Action onChanged)
    {
        InitializeComponent();
        _rules = rules;
        _onChanged = onChanged;
        RefreshList();
    }

    private void RefreshList()
    {
        RuleList.ItemsSource = null;
        RuleList.ItemsSource = _rules.OrderBy(r => r.Order).ToList();
    }

    private void OnAddRule(object sender, RoutedEventArgs e)
    {
        var name = NewName.Text.Trim();
        var pattern = NewPattern.Text.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern)) return;

        _rules.Add(new LinkRule
        {
            Name = name,
            Pattern = pattern,
            UrlTemplate = "{match}",
            Order = _rules.Count,
        });
        NewName.Text = string.Empty;
        NewPattern.Text = string.Empty;
        _onChanged();
        RefreshList();
    }

    private void OnRuleFieldChanged(object sender, RoutedEventArgs e) => _onChanged();

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LinkRule rule)
        {
            var list = _rules.OrderBy(r => r.Order).ToList();
            var idx = list.IndexOf(rule);
            if (idx <= 0) return;
            list[idx].Order--;
            list[idx - 1].Order++;
            _onChanged();
            RefreshList();
        }
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LinkRule rule)
        {
            var list = _rules.OrderBy(r => r.Order).ToList();
            var idx = list.IndexOf(rule);
            if (idx >= list.Count - 1) return;
            list[idx].Order++;
            list[idx + 1].Order--;
            _onChanged();
            RefreshList();
        }
    }

    private void OnDeleteRule(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LinkRule rule)
        {
            _rules.Remove(rule);
            var ordered = _rules.OrderBy(r => r.Order).ToList();
            for (int i = 0; i < ordered.Count; i++) ordered[i].Order = i;
            _onChanged();
            RefreshList();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
