$(document).ready(function () {
    GetWeekly('Claim', 'GetWeeklyClaim', 'container-claim');
    GetWeeklyTat('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    GetWeeklyPie('Claim', 'GetWeeklyClaim', 'container-claim-pie');
    GetWeeklyPie('Agency ', 'GetAgentClaim', 'container-agency-pie');

    GetChart('Claim', 'GetClaimChart', 'container-monthly-claim')

    $("#btnWeeklyReport").click(function () {
        GetWeekly('Claim', 'GetWeeklyClaim', 'container-claim');
    })

    $("#btnMonthlyReport").click(function () {
        GetMonthly('Claim', 'GetMonthlyClaim', 'container-claim');
    })
    $("#btnWeeklyPie").click(function () {
        GetWeeklyPie('Claim', 'GetWeeklyClaim', 'container-claim-pie');
    })
    $("#btnMonthlyPie").click(function () {
        GetMonthlyPie('Claim', 'GetMonthlyClaim', 'container-claim-pie');
    })
    $("#btnWeeklyTat").click(function () {
        GetWeeklyTat('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    })
    $("#btnMonthlyTat").click(function () {
        //GetMonthly('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    })

    $('.details-page').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('.details-page').attr('disabled', 'disabled');
        $('html').css('cursor', 'not-allowed');

        var nodes = document.getElementById("section").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
});