using System.ComponentModel.DataAnnotations;

public class EventFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Location { get; set; }
    public List<string>? Tags { get; set; }
    public bool? HasAvailableSpots { get; set; }
    [Range(1, 100)]
    public int? Page { get; set; }
    [Range(1, 50)]
    public int? PageSize { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }
}