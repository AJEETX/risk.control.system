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
});