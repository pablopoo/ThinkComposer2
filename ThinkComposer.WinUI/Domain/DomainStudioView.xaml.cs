using Instrumind.ThinkComposer.Core.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Instrumind.ThinkComposer.WinUI.Domain;

public sealed partial class DomainStudioView : UserControl
{
    private const string TemplateGroup = "Template";
    private readonly DomainGroupItem[] _groups =
    [
        new("Concept definitions", CompositionDefinitionGroup.Concept),
        new("Relationship definitions", CompositionDefinitionGroup.Relationship),
        new("Link-role definitions", CompositionDefinitionGroup.LinkRole),
        new("Marker definitions", CompositionDefinitionGroup.Marker),
        new("Table definitions", CompositionDefinitionGroup.Table),
        new("External languages", CompositionDefinitionGroup.ExternalLanguage),
        new("Generation templates", null)
    ];

    public DomainStudioView()
    {
        InitializeComponent();
        GroupBox.ItemsSource = _groups;
        GroupBox.SelectedIndex = 0;
    }

    public event EventHandler<CompositionDocumentSnapshot>? DocumentChanged;

    public event EventHandler? CloseRequested;

    public CompositionDocumentSnapshot? Document { get; private set; }

    public void LoadDocument(CompositionDocumentSnapshot? document)
    {
        Document = document;
        RefreshList();
    }

    private void GroupBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshList();
    }

    private void DefinitionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DefinitionsList.SelectedItem is not DomainEntry entry)
        {
            return;
        }

        IdBox.Text = entry.Id;
        NameBox.Text = entry.Name;
        KindBox.Text = entry.Kind;
        SummaryBox.Text = entry.Summary;
        FillBox.Text = entry.Style.Fill;
        StrokeBox.Text = entry.Style.Stroke;
        TextColorBox.Text = entry.Style.Text;
        StrokeThicknessBox.Value = entry.Style.StrokeThickness;
        ValueBox.Text = entry.Value;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        DefinitionsList.SelectedItem = null;
        IdBox.Text = string.Empty;
        NameBox.Text = string.Empty;
        KindBox.Text = SelectedGroup()?.DefinitionGroup?.ToString() ?? TemplateGroup;
        SummaryBox.Text = string.Empty;
        FillBox.Text = string.Empty;
        StrokeBox.Text = string.Empty;
        TextColorBox.Text = string.Empty;
        StrokeThicknessBox.Value = 0;
        ValueBox.Text = string.Empty;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (Document is null || SelectedGroup() is not { } group || string.IsNullOrWhiteSpace(IdBox.Text))
        {
            return;
        }

        if (group.DefinitionGroup is null)
        {
            Document = CompositionDocumentSnapshotEditor.UpsertDomainTemplate(
                Document,
                new CompositionExtensionSnapshot(IdBox.Text.Trim(), ValueBox.Text));
        }
        else
        {
            var style = new CompositionStyleSnapshot(
                FillBox.Text.Trim(),
                StrokeBox.Text.Trim(),
                TextColorBox.Text.Trim(),
                double.IsNaN(StrokeThicknessBox.Value) ? 0 : Math.Max(0, StrokeThicknessBox.Value));
            var definition = new CompositionDefinitionSnapshot(
                IdBox.Text.Trim(),
                string.IsNullOrWhiteSpace(NameBox.Text) ? IdBox.Text.Trim() : NameBox.Text.Trim(),
                string.IsNullOrWhiteSpace(KindBox.Text) ? group.DefinitionGroup.Value.ToString() : KindBox.Text.Trim(),
                SummaryBox.Text.Trim(),
                style);
            Document = CompositionDocumentSnapshotEditor.UpsertDefinition(Document, group.DefinitionGroup.Value, definition);
        }

        DocumentChanged?.Invoke(this, Document);
        RefreshList();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (Document is null || SelectedGroup() is not { } group || string.IsNullOrWhiteSpace(IdBox.Text))
        {
            return;
        }

        var id = IdBox.Text.Trim();
        Document = group.DefinitionGroup is null
            ? CompositionDocumentSnapshotEditor.DeleteDomainTemplate(Document, id)
            : CompositionDocumentSnapshotEditor.DeleteDefinition(Document, group.DefinitionGroup.Value, id);
        DocumentChanged?.Invoke(this, Document);
        RefreshList();
        NewButton_Click(sender, e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshList()
    {
        if (Document is null || SelectedGroup() is not { } group)
        {
            DefinitionsList.ItemsSource = Array.Empty<DomainEntry>();
            return;
        }

        DefinitionsList.ItemsSource = group.DefinitionGroup is null
            ? Document.Domain.Templates.Select(template => new DomainEntry(
                template.Key,
                template.Key,
                TemplateGroup,
                string.Empty,
                new CompositionStyleSnapshot(),
                template.Value)).ToArray()
            : GetDefinitions(Document.Domain, group.DefinitionGroup.Value)
                .Select(definition => new DomainEntry(
                    definition.Id,
                    definition.Name,
                    definition.Kind,
                    definition.Summary,
                    definition.Style,
                    definition.TableRecords.Rows.Count == 0
                        ? string.Empty
                        : $"{definition.TableRecords.Rows.Count} table row(s)")).ToArray();
    }

    private DomainGroupItem? SelectedGroup()
    {
        return GroupBox.SelectedItem as DomainGroupItem;
    }

    private static IReadOnlyList<CompositionDefinitionSnapshot> GetDefinitions(
        CompositionDomainSnapshot domain,
        CompositionDefinitionGroup group)
    {
        return group switch
        {
            CompositionDefinitionGroup.Concept => domain.ConceptDefinitions,
            CompositionDefinitionGroup.Relationship => domain.RelationshipDefinitions,
            CompositionDefinitionGroup.LinkRole => domain.LinkRoleDefinitions,
            CompositionDefinitionGroup.Marker => domain.MarkerDefinitions,
            CompositionDefinitionGroup.Table => domain.TableDefinitions,
            CompositionDefinitionGroup.ExternalLanguage => domain.ExternalLanguages,
            _ => Array.Empty<CompositionDefinitionSnapshot>()
        };
    }

    private sealed record DomainGroupItem(string Name, CompositionDefinitionGroup? DefinitionGroup)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    private sealed record DomainEntry(
        string Id,
        string Name,
        string Kind,
        string Summary,
        CompositionStyleSnapshot Style,
        string Value)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Id : Name;
        }
    }
}
