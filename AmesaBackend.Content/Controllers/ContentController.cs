using AmesaBackend.Content.Data;
using AmesaBackend.Content.DTOs;
using AmesaBackend.Content.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Content.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ContentController : ControllerBase
    {
        private readonly ContentDbContext _context;
        private readonly ILogger<ContentController> _logger;

        public ContentController(ContentDbContext context, ILogger<ContentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<ContentItemDto>>> GetContent([FromQuery] ContentFilters filters)
        {
            try
            {
                var page = filters.Page > 0 ? filters.Page : 1;
                var pageSize = filters.PageSize > 0 && filters.PageSize <= 100 ? filters.PageSize : 20;

                var query = _context.Contents
                    .AsNoTracking()
                    .Where(c => c.Status == ContentStatus.Published && c.DeletedAt == null);

                if (!string.IsNullOrWhiteSpace(filters.ContentType))
                {
                    query = query.Where(c => c.MetaTitle == filters.ContentType || c.Excerpt == filters.ContentType);
                }

                if (!string.IsNullOrWhiteSpace(filters.Category))
                {
                    query = query.Where(c => c.Category != null && c.Category.Slug == filters.Category);
                }

                if (!string.IsNullOrWhiteSpace(filters.Language))
                {
                    query = query.Where(c => c.Language == filters.Language);
                }

                var total = await query.CountAsync();

                var items = await query
                    .OrderByDescending(c => c.PublishedAt ?? c.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ContentItemDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Slug = c.Slug,
                        Excerpt = c.Excerpt,
                        ContentBody = c.ContentBody,
                        ContentType = c.MetaTitle, // using MetaTitle as content type placeholder
                        Category = c.Category != null ? c.Category.Slug : null,
                        Language = c.Language,
                        PublishedAt = c.PublishedAt,
                        UpdatedAt = c.UpdatedAt,
                        FeaturedImageUrl = c.FeaturedImageUrl
                    })
                    .ToListAsync();

                return Ok(new PagedResponse<ContentItemDto>
                {
                    Success = true,
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    Total = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving content list");
                return StatusCode(500, new PagedResponse<ContentItemDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "Failed to retrieve content" }
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ContentItemDto>>> GetContentById(Guid id)
        {
            try
            {
                var content = await _context.Contents
                    .AsNoTracking()
                    .Include(c => c.Category)
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == ContentStatus.Published && c.DeletedAt == null);

                if (content == null)
                {
                    return NotFound(new ApiResponse<ContentItemDto>
                    {
                        Success = false,
                        Message = "Content not found",
                        Error = new ErrorResponse { Code = "NOT_FOUND", Message = "Content not found" }
                    });
                }

                var dto = new ContentItemDto
                {
                    Id = content.Id,
                    Title = content.Title,
                    Slug = content.Slug,
                    Excerpt = content.Excerpt,
                    ContentBody = content.ContentBody,
                    ContentType = content.MetaTitle, // placeholder
                    Category = content.Category?.Slug,
                    Language = content.Language,
                    PublishedAt = content.PublishedAt,
                    UpdatedAt = content.UpdatedAt,
                    FeaturedImageUrl = content.FeaturedImageUrl
                };

                return Ok(new ApiResponse<ContentItemDto>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving content by id {Id}", id);
                return StatusCode(500, new ApiResponse<ContentItemDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "Failed to retrieve content" }
                });
            }
        }
    }
}

