$(function () {
    $('#loadTemplate').on('click', function () {
        const container = $("#reportTemplateContainer");
        const caseId = $("#caseIdHidden").val();

        // Load only if not already loaded
        if (container.data("loaded")) return;

        container.html("<div class='text-center p-3'><i class='fas fa-sync fa-spin fa-2x'></i></div>");

        $.get("/CaseActive/GetReportTemplate", { caseId: caseId })
            .done(function (html) {
                const safe = DOMPurify.sanitize(html, { RETURN_TRUSTED_TYPE: false });
                container.html(safe);
                container.data("loaded", true);
            })
            .fail(function () {
                container.html("<div class='alert alert-danger'>Failed to load report template.</div>");
            });
    });
});
