//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//using risk.control.system.Services;

//namespace risk.control.system.Controllers.Api
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class DbController : ControllerBase
//    {
//        private readonly ISqliteSchemaService service;

//        public DbController(ISqliteSchemaService service)
//        {
//            this.service = service;
//        }
//        [HttpGet("")]
//        [AllowAnonymous]
//        public IActionResult GetSchemaAndData()
//        {
//            var result = new Dictionary<string, object>();
//            var tables = service.GetAllTables();
//            //exclude tables 
//            // PinCode, District, State, Country
//            //AspNetRoleClaims, ApsNetRoles, ApsNetUserClaimsAspNetRoleLogins, AspNetRoleUsers,AspNetRoleTokens, AuditLogs,
//            foreach (var table in tables)
//            {
//                var schema = service.GetTableSchema(table)
//                    .Select(col => new
//                    {
//                        column = col.ColumnName,
//                        type = col.DataType,
//                        notnull = col.NotNull
//                    })
//                    .ToList();

//                var data = service.GetTableData(table);

//                result[table] = new
//                {
//                    schema,
//                    data
//                };
//            }

//            return Ok(result);
//        }
//    }
//}
