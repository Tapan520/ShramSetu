using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShramSetu.Core.Entities;
using ShramSetu.Data;

namespace ShramSetu.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public IList<SkillCategory> Categories { get; set; } = new List<SkillCategory>();

        public async Task OnGetAsync()
        {
            Categories = await _db.SkillCategories.OrderBy(c => c.Name).ToListAsync();
        }
    }
}
