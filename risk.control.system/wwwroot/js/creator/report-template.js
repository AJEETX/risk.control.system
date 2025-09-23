$(function () {
    $("#loadTemplate").on("click", function (e) {
        e.preventDefault();

        let caseId = $("#caseIdHidden").val();
        let container = $("#reportTemplateContainer");

        container.html("<div class='text-center p-3'><i class='fas fa-sync fa-spin'></i>...</div>");

        $.get("/CaseActive/GetReportTemplate", { caseId: caseId })
            .done(function (html) {
                container.html(html);
            })
            .fail(function () {
                container.html("<div class='alert alert-danger'>Failed to load report template.</div>");
            });
    });
});