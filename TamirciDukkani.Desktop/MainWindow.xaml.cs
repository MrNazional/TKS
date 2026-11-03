using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TamirciDukkani.Desktop;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TamirciDukkani.Desktop.Data;
using TamirciDukkani.Desktop.Models;
using CsvHelper;
using ClosedXML.Excel;
using Microsoft.Win32;

public partial class MainWindow : Window
{
    private readonly AppDbContext _db = new AppDbContext();
    private ObservableCollection<Vehicle> _vehicles = new ObservableCollection<Vehicle>();
    private Vehicle? _selected;
    private ObservableCollection<JobView> _jobs = new ObservableCollection<JobView>();
    private JobView? _selectedJob;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            _db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veritabanı oluşturulamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        LoadVehicles();
        LoadJobs();
        LoadDashboard();
    }

    private void LoadVehicles(string? search = null, string? brand = null)
    {
        var query = _db.Vehicles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(v => v.Plaka.ToLower().Contains(s)
                                   || v.Marka.ToLower().Contains(s)
                                   || v.Model.ToLower().Contains(s)
                                   || v.MusteriAdi.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(brand))
        {
            var b = brand.Trim().ToLowerInvariant();
            query = query.Where(v => v.Marka.ToLower().Contains(b));
        }

        _vehicles = new ObservableCollection<Vehicle>(query.OrderBy(v => v.Marka).ThenBy(v => v.Model).ThenBy(v => v.Plaka).ToList());
        VehiclesGrid.ItemsSource = _vehicles;
        // Also refresh vehicle combo for jobs form
        JobVehicleCombo.ItemsSource = _vehicles;
    }

    private void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = VehiclesGrid.SelectedItem as Vehicle;
        if (_selected == null)
        {
            ClearForm();
            return;
        }

        PlakaText.Text = _selected.Plaka;
        MarkaText.Text = _selected.Marka;
        ModelText.Text = _selected.Model;
        ModelYiliText.Text = _selected.ModelYili?.ToString() ?? string.Empty;
        MusteriAdiText.Text = _selected.MusteriAdi;
        MusteriTelText.Text = _selected.MusteriTel ?? string.Empty;
        SasiNoText.Text = _selected.SasiNo ?? string.Empty;
        NotlarText.Text = _selected.Notlar ?? string.Empty;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        VehiclesGrid.UnselectAll();
        _selected = null;
        ClearForm();
        PlakaText.Focus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PlakaText.Text))
        {
            MessageBox.Show("Plaka zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int? modelYili = null;
        if (!string.IsNullOrWhiteSpace(ModelYiliText.Text))
        {
            if (int.TryParse(ModelYiliText.Text, out var y)) modelYili = y; else
            {
                MessageBox.Show("Model yılı sayı olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            if (_selected == null)
            {
                var v = new Vehicle
                {
                    Plaka = PlakaText.Text.Trim(),
                    Marka = MarkaText.Text.Trim(),
                    Model = ModelText.Text.Trim(),
                    ModelYili = modelYili,
                    MusteriAdi = MusteriAdiText.Text.Trim(),
                    MusteriTel = string.IsNullOrWhiteSpace(MusteriTelText.Text) ? null : MusteriTelText.Text.Trim(),
                    SasiNo = string.IsNullOrWhiteSpace(SasiNoText.Text) ? null : SasiNoText.Text.Trim(),
                    Notlar = string.IsNullOrWhiteSpace(NotlarText.Text) ? null : NotlarText.Text.Trim()
                };
                _db.Vehicles.Add(v);
                _db.SaveChanges();
                _vehicles.Add(v);
                VehiclesGrid.SelectedItem = v;
            }
            else
            {
                var v = _db.Vehicles.FirstOrDefault(x => x.Id == _selected.Id);
                if (v == null) return;
                v.Plaka = PlakaText.Text.Trim();
                v.Marka = MarkaText.Text.Trim();
                v.Model = ModelText.Text.Trim();
                v.ModelYili = modelYili;
                v.MusteriAdi = MusteriAdiText.Text.Trim();
                v.MusteriTel = string.IsNullOrWhiteSpace(MusteriTelText.Text) ? null : MusteriTelText.Text.Trim();
                v.SasiNo = string.IsNullOrWhiteSpace(SasiNoText.Text) ? null : SasiNoText.Text.Trim();
                v.Notlar = string.IsNullOrWhiteSpace(NotlarText.Text) ? null : NotlarText.Text.Trim();
                _db.SaveChanges();

                // Refresh selection item
                var idx = _vehicles.IndexOf(_vehicles.First(x => x.Id == v.Id));
                _vehicles[idx] = v;
                VehiclesGrid.Items.Refresh();
                VehiclesGrid.SelectedItem = v;
            }

            MessageBox.Show("Kayıt kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null)
        {
            MessageBox.Show("Silmek için bir kayıt seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = MessageBox.Show("Seçili aracı silmek istiyor musunuz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            var v = _db.Vehicles.FirstOrDefault(x => x.Id == _selected.Id);
            if (v != null)
            {
                _db.Vehicles.Remove(v);
                _db.SaveChanges();
                _vehicles.Remove(_vehicles.First(x => x.Id == v.Id));
                ClearForm();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadVehicles(SearchTextBox.Text, BrandFilterTextBox.Text);
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        LoadVehicles(SearchTextBox.Text, BrandFilterTextBox.Text);
    }

    private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = string.Empty;
        BrandFilterTextBox.Text = string.Empty;
        LoadVehicles();
    }

    private void ClearForm()
    {
        PlakaText.Text = string.Empty;
        MarkaText.Text = string.Empty;
        ModelText.Text = string.Empty;
        ModelYiliText.Text = string.Empty;
        MusteriAdiText.Text = string.Empty;
        MusteriTelText.Text = string.Empty;
        SasiNoText.Text = string.Empty;
        NotlarText.Text = string.Empty;
    }

    // Jobs
    private void LoadJobs(string? search = null, string? status = null)
    {
        var jobsQuery = _db.Jobs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "(Tümü)")
        {
            jobsQuery = jobsQuery.Where(j => j.Durum == status);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            // Search across job fields and vehicle plate/customer
            var vehicleIds = _db.Vehicles
                .Where(v => v.Plaka.ToLower().Contains(s) || v.MusteriAdi.ToLower().Contains(s) || v.Marka.ToLower().Contains(s))
                .Select(v => v.Id)
                .ToList();
            jobsQuery = jobsQuery.Where(j => vehicleIds.Contains(j.VehicleId)
                                           || (j.Aciklama != null && j.Aciklama.ToLower().Contains(s)));
        }

        var list = (from j in jobsQuery.ToList()
                    join v in _db.Vehicles.ToList() on j.VehicleId equals v.Id into vv
                    from v in vv.DefaultIfEmpty()
                    select new JobView
                    {
                        Id = j.Id,
                        VehicleId = j.VehicleId,
                        VehicleDisplay = v != null ? $"{v.Plaka} ({v.Marka} {v.Model})" : $"Araç #{j.VehicleId}",
                        GelisTarihi = j.GelisTarihi,
                        CikisTarihi = j.CikisTarihi,
                        Durum = j.Durum,
                        Aciklama = j.Aciklama,
                        IscilikTutar = j.IscilikTutar,
                        ParcaTutar = j.ParcaTutar,
                        GenelTutar = j.GenelTutar
                    }).OrderByDescending(x => x.GelisTarihi).ToList();

        _jobs = new ObservableCollection<JobView>(list);
        JobsGrid.ItemsSource = _jobs;

        // Initialize defaults
        if (JobGelisDate.SelectedDate == null) JobGelisDate.SelectedDate = DateTime.Today;
    }

    private void JobsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedJob = JobsGrid.SelectedItem as JobView;
        if (_selectedJob == null)
        {
            JobNewButton_Click(sender, new RoutedEventArgs());
            return;
        }

        JobVehicleCombo.SelectedValue = _selectedJob.VehicleId;
        SelectComboByContent(JobStatusCombo, _selectedJob.Durum);
        JobGelisDate.SelectedDate = _selectedJob.GelisTarihi;
        JobCikisDate.SelectedDate = _selectedJob.CikisTarihi;
        JobAciklamaText.Text = _selectedJob.Aciklama ?? string.Empty;
        JobIscilikText.Text = _selectedJob.IscilikTutar?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        JobParcaText.Text = _selectedJob.ParcaTutar?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        JobGenelText.Text = _selectedJob.GenelTutar?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void JobNewButton_Click(object sender, RoutedEventArgs e)
    {
        JobsGrid.UnselectAll();
        _selectedJob = null;
        if (JobVehicleCombo.Items.Count > 0)
            JobVehicleCombo.SelectedIndex = 0;
        SelectComboByContent(JobStatusCombo, "Atölyede");
        JobGelisDate.SelectedDate = DateTime.Today;
        JobCikisDate.SelectedDate = null;
        JobAciklamaText.Text = string.Empty;
        JobIscilikText.Text = string.Empty;
        JobParcaText.Text = string.Empty;
        JobGenelText.Text = string.Empty;
    }

    private void JobSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (JobVehicleCombo.SelectedValue == null)
        {
            MessageBox.Show("Araç seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var vehicleId = (int)JobVehicleCombo.SelectedValue;
        var durum = ((ComboBoxItem)JobStatusCombo.SelectedItem).Content?.ToString() ?? "Atölyede";
        var gelis = JobGelisDate.SelectedDate ?? DateTime.Today;
        var cikis = JobCikisDate.SelectedDate;

        decimal? parseDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (decimal.TryParse(s.Replace(" ", string.Empty), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            if (decimal.TryParse(s, out d)) return d;
            return null;
        }

        var iscilik = parseDec(JobIscilikText.Text);
        var parca = parseDec(JobParcaText.Text);
        var genel = parseDec(JobGenelText.Text);

        try
        {
            if (_selectedJob == null)
            {
                var j = new Job
                {
                    VehicleId = vehicleId,
                    Durum = durum,
                    GelisTarihi = gelis,
                    CikisTarihi = cikis,
                    Aciklama = string.IsNullOrWhiteSpace(JobAciklamaText.Text) ? null : JobAciklamaText.Text.Trim(),
                    IscilikTutar = iscilik,
                    ParcaTutar = parca,
                    GenelTutar = genel
                };
                _db.Jobs.Add(j);
                _db.SaveChanges();
            }
            else
            {
                var j = _db.Jobs.FirstOrDefault(x => x.Id == _selectedJob.Id);
                if (j == null) return;
                j.VehicleId = vehicleId;
                j.Durum = durum;
                j.GelisTarihi = gelis;
                j.CikisTarihi = cikis;
                j.Aciklama = string.IsNullOrWhiteSpace(JobAciklamaText.Text) ? null : JobAciklamaText.Text.Trim();
                j.IscilikTutar = iscilik;
                j.ParcaTutar = parca;
                j.GenelTutar = genel;
                _db.SaveChanges();
            }

            LoadJobs(JobsSearchTextBox.Text, GetComboContent(JobsStatusCombo));
            LoadDashboard();
            MessageBox.Show("İş emri kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void JobDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedJob == null)
        {
            MessageBox.Show("Silmek için iş seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var res = MessageBox.Show("Seçili işi silmek istiyor musunuz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            var j = _db.Jobs.FirstOrDefault(x => x.Id == _selectedJob.Id);
            if (j != null)
            {
                _db.Jobs.Remove(j);
                _db.SaveChanges();
                LoadJobs(JobsSearchTextBox.Text, GetComboContent(JobsStatusCombo));
                LoadDashboard();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void JobRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadJobs(JobsSearchTextBox.Text, GetComboContent(JobsStatusCombo));
        LoadDashboard();
    }

    private void JobsFilterButton_Click(object sender, RoutedEventArgs e)
    {
        LoadJobs(JobsSearchTextBox.Text, GetComboContent(JobsStatusCombo));
    }

    private void JobsClearFilterButton_Click(object sender, RoutedEventArgs e)
    {
        JobsSearchTextBox.Text = string.Empty;
        JobsStatusCombo.SelectedIndex = 0; // (Tümü)
        LoadJobs();
    }

    private static void SelectComboByContent(ComboBox combo, string content)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem cbi && string.Equals(cbi.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = cbi;
                break;
            }
        }
    }

    private static string? GetComboContent(ComboBox combo)
    {
        return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    }

    private void LoadDashboard()
    {
        try
        {
            var inShop = _db.Jobs.Count(j => j.Durum == "Atölyede");
            var today = DateTime.Today;
            var deliveries = _db.Jobs.Count(j => j.CikisTarihi != null && j.CikisTarihi.Value.Date == today);
            InShopCountText.Text = $"Atölyede: {inShop}";
            TodayDeliveriesText.Text = $"Bugün Teslim: {deliveries}";
        }
        catch { /* ignore */ }
    }

    // Exports
    private void ExportVehiclesCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Araçları CSV olarak kaydet",
            FileName = $"araclar-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            DefaultExt = ".csv",
            Filter = "CSV dosyası (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != true) return;

        using var writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_db.Vehicles.OrderBy(v => v.Plaka).ToList());
        MessageBox.Show($"CSV kaydedildi: {dlg.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportVehiclesExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Araçları Excel olarak kaydet",
            FileName = $"araclar-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx",
            DefaultExt = ".xlsx",
            Filter = "Excel dosyası (*.xlsx)|*.xlsx|Tüm dosyalar (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != true) return;

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Araçlar");
        var data = _db.Vehicles.OrderBy(v => v.Plaka).ToList();
        ws.Cell(1, 1).Value = "Plaka"; ws.Cell(1, 2).Value = "Marka"; ws.Cell(1, 3).Value = "Model"; ws.Cell(1, 4).Value = "ModelYili"; ws.Cell(1, 5).Value = "MusteriAdi"; ws.Cell(1, 6).Value = "MusteriTel";
        int r = 2;
        foreach (var v in data)
        {
            ws.Cell(r, 1).Value = v.Plaka;
            ws.Cell(r, 2).Value = v.Marka;
            ws.Cell(r, 3).Value = v.Model;
            ws.Cell(r, 4).Value = v.ModelYili;
            ws.Cell(r, 5).Value = v.MusteriAdi;
            ws.Cell(r, 6).Value = v.MusteriTel;
            r++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(dlg.FileName);
        MessageBox.Show($"Excel kaydedildi: {dlg.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportJobsCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "İşleri CSV olarak kaydet",
            FileName = $"isler-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            DefaultExt = ".csv",
            Filter = "CSV dosyası (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != true) return;

        using var writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_jobs);
        MessageBox.Show($"CSV kaydedildi: {dlg.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportJobsExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "İşleri Excel olarak kaydet",
            FileName = $"isler-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx",
            DefaultExt = ".xlsx",
            Filter = "Excel dosyası (*.xlsx)|*.xlsx|Tüm dosyalar (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != true) return;

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("İşler");
        ws.Cell(1, 1).Value = "Araç"; ws.Cell(1, 2).Value = "Gelis"; ws.Cell(1, 3).Value = "Cikis"; ws.Cell(1, 4).Value = "Durum"; ws.Cell(1, 5).Value = "Aciklama"; ws.Cell(1, 6).Value = "Iscilik"; ws.Cell(1, 7).Value = "Parca"; ws.Cell(1, 8).Value = "Genel";
        int r = 2;
        foreach (var j in _jobs)
        {
            ws.Cell(r, 1).Value = j.VehicleDisplay;
            ws.Cell(r, 2).Value = j.GelisTarihi;
            ws.Cell(r, 3).Value = j.CikisTarihi;
            ws.Cell(r, 4).Value = j.Durum;
            ws.Cell(r, 5).Value = j.Aciklama;
            ws.Cell(r, 6).Value = j.IscilikTutar;
            ws.Cell(r, 7).Value = j.ParcaTutar;
            ws.Cell(r, 8).Value = j.GenelTutar;
            r++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(dlg.FileName);
        MessageBox.Show($"Excel kaydedildi: {dlg.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // View model for jobs grid
    private class JobView
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleDisplay { get; set; } = string.Empty;
        public DateTime GelisTarihi { get; set; }
        public DateTime? CikisTarihi { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public decimal? IscilikTutar { get; set; }
        public decimal? ParcaTutar { get; set; }
        public decimal? GenelTutar { get; set; }
    }
}