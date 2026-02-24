namespace risk.control.system.StartupExtensions
{
    public static class AddBundleStaticFilesExtension
    {
        public static IServiceCollection AddBundleFiles(this IServiceCollection services)
        {
            services.AddWebOptimizer(pipeline =>
            {
                try
                {
                    var cssBundle = pipeline.AddCssBundle("/dist/app.min.css",
                        "css/adminlte.css",
                        "plugins/icheck-bootstrap/icheck-bootstrap.css",
                        "css/jquery-ui.css",
                        "css/jquery-confirm.css",
                        "css/bootstrap-toggle.css",
                        "css/site.css",
                        "css/cookie.css",
                        "plugins/datatables-bs4/css/dataTables.bootstrap4.css",
                        "plugins/datatables-responsive/css/responsive.bootstrap4.css",
                        "plugins/datatables-buttons/css/buttons.bootstrap4.css"
                    );
                    cssBundle.AdjustRelativePaths(); // If this still errors, try: .MinifyCss() only
                    cssBundle.MinifyCss();

                    var jsBundle = pipeline.AddJavaScriptBundle("/dist/app.min.js",
                        "js/jquery-3.7.1.js",
                        "plugins/bootstrap/js/bootstrap.bundle.js",
                        "js/adminlte.js",
                        "js/jqueryconfirm/dist/jquery-confirm.js",
                        "js/jqueryconfirm/js/custom.jqueryconfirm.js",
                        "js/jquery-ui.js",
                        "js/bootstrap-toggle.js",
                        "js/ua-parser.js",
                        "plugins/form/jquery.form.min.js",
                        "plugins/datatables/jquery.dataTables.js",
                        "plugins/datatables-bs4/js/dataTables.bootstrap4.js",
                        "plugins/datatables-responsive/js/dataTables.responsive.js",
                        "plugins/datatables-responsive/js/responsive.bootstrap4.js",
                        "plugins/datatables-buttons/js/dataTables.buttons.js",
                        "plugins/datatables-buttons/js/buttons.bootstrap4.js",
                        "plugins/datatables-buttons/js/buttons.html5.js",
                        "plugins/datatables-buttons/js/buttons.print.js",
                        "plugins/datatables-buttons/js/buttons.colVis.js",
                        "plugins/datatables/dataTables.fixedColumns.js",
                        "plugins/datatables/fixedColumns.dataTables.js",
                        "js/site.js",
                        "js/clock.js",
                        "js/common/datatable-error.js"
                        );
                    jsBundle.MinifyJavaScript();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Bundle error: " + ex.Message);
                }
            }, options =>
            {
                options.EnableCaching = true; // Disables the server-side memory cache for assets
            });

            services.AddResponseCompression();
            return services;
        }
    }
}