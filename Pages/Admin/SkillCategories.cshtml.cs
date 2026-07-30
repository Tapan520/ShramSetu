using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SkillCategoriesModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public SkillCategoriesModel(ApplicationDbContext db) => _db = db;

    public List<CategoryRow> Categories { get; set; } = [];

    [BindProperty]
    public CategoryInput Input { get; set; } = new();

    public async Task OnGetAsync(Guid? editId)
    {
        await LoadCategoriesAsync();
        if (editId.HasValue)
        {
            var cat = await _db.SkillCategories.FindAsync(editId.Value);
            if (cat is not null) Input = new CategoryInput { Id = cat.Id, Name = cat.Name, IconCssClass = cat.IconCssClass };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadCategoriesAsync(); return Page(); }

        if (Input.Id == Guid.Empty)
        {
            _db.SkillCategories.Add(new SkillCategory { Id = Guid.NewGuid(), Name = Input.Name, IconCssClass = Input.IconCssClass });
        }
        else
        {
            var cat = await _db.SkillCategories.FindAsync(Input.Id);
            if (cat is not null) { cat.Name = Input.Name; cat.IconCssClass = Input.IconCssClass; }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Skill category saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var cat = await _db.SkillCategories.FindAsync(id);
        if (cat is not null) { _db.SkillCategories.Remove(cat); await _db.SaveChangesAsync(); }
        TempData["Success"] = "Category deleted.";
        return RedirectToPage();
    }

    private async Task LoadCategoriesAsync()
        => Categories = await _db.SkillCategories
            .Select(c => new CategoryRow
            {
                Id = c.Id, Name = c.Name, IconCssClass = c.IconCssClass,
                WorkerCount = c.Workers.Count(w => !w.IsDeleted),
                JobCount = c.JobPosts.Count(j => j.Status == Core.Enums.JobPostStatus.Open)
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

    public class CategoryRow { public Guid Id; public string Name = ""; public string IconCssClass = ""; public int WorkerCount; public int JobCount; }
    public class CategoryInput
    {
        public Guid Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string IconCssClass { get; set; } = "bi bi-tools";
    }
}
