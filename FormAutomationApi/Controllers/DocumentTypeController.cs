using FormAutomationApi.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormAutomationApi.Controllers
{
    public class DocumentTypeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DocumentTypeController(ApplicationDbContext context) {
            _context = context;
        }

        [HttpGet]
        [Route("api/[controller]")]
        public async Task<IActionResult> Index()
        {
            var docmentTypes = await _context.DocumentTypes.ToListAsync();
            return Ok(docmentTypes);
        }
    }
}
