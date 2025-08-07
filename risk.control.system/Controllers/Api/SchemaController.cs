using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchemaController : ControllerBase
    {
        private readonly ISqliteSchemaService service;

        public SchemaController(ISqliteSchemaService service)
        {
            this.service = service;
        }
        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSchema()
        {
            var tables = service.GetAllTables();
            var schema = new Dictionary<string, object>();

            foreach (var table in tables)
            {
                var columns = service.GetTableSchema(table)
               .Select(col => new
               {
                   column = col.ColumnName,
                   type = col.DataType,
                   notnull = col.NotNull
               })
               .ToList();

                schema[table] = columns;
            }
            return Ok(schema);
        }
    }
}
