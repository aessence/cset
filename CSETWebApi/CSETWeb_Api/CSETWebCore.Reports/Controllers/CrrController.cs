﻿using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.XPath;
using CSETWebCore.Interfaces.Assessment;
using CSETWebCore.Interfaces.Crr;
using CSETWebCore.Interfaces.Helpers;
using CSETWebCore.Interfaces.Maturity;
using CSETWebCore.Reports.Helper;
using CSETWebCore.Business.Reports;
using CSETWebCore.Reports.Models;
using CSETWebCore.Interfaces.Reports;
using CSETWebCore.DataLayer.Model;
using IronPdf;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using CSETWebCore.Interfaces.Demographic;


namespace CSETWebCore.Reports.Controllers
{
    public class CrrController : Controller
    {
        private readonly IViewEngine _engine;
        private readonly ITokenManager _token;
        private readonly IAssessmentBusiness _assessment;
        private readonly IDemographicBusiness _demographic;
        private readonly IReportsDataBusiness _report;
        private readonly CSETContext _context;
        private readonly IMaturityBusiness _maturity;
        private readonly ICrrScoringHelper _crr;


        private readonly IDictionary<string, string> _viewToTitle = new Dictionary<string, string>
        {
            {"_crrcoversheet", "Top"},
            {"_crrcoversheet2", "Title"},
            {"_crrintroabout", "Introduction" },
            {"_crrmil1performancesummary", "CRR MIL-1 Performance Summary" },
            {"_crrperformancesummary", "CRR Performance Summary" },
            {"_crrnistcsfsummary", "NIST Cybersecurity Framework Summary" },
            {"_crrmil1performance", "CRR MIL-1 Performance" },
            {"_crrpercentageofpractices", "Percentage of Practices Completed by Domain" },
            {"_crrperformanceappendixa", "Appendix A" },
            {"_crrdomaindetail", "Asset Management" },
        };


        public CrrController(IViewEngine engine, ITokenManager token,
            IAssessmentBusiness assessment,
            IDemographicBusiness demographic,
            CSETContext context, IReportsDataBusiness report, IMaturityBusiness maturity, ICrrScoringHelper crr)
        {
            _engine = engine;
            _token = token;
            _assessment = assessment;
            _demographic = demographic;
            _report = report;
            _context = context;
            _maturity = maturity;
            _crr = crr;
        }

        public IActionResult Index()
        {
            TempData["links"] = UrlStringHelper.GetBaseUrl(Request);
            return View();
        }


        
        [HttpGet]
        [Route("getPdf")]
        public async Task<IActionResult> CreatePdf(string view)
        {
            try
            {
                byte[] sessionToken = null;
                byte[] securityTemp = null;
                string security = "None";
                if (HttpContext.Session.TryGetValue("token", out sessionToken))
                {
                    _token.Init(Encoding.ASCII.GetString(sessionToken));
                }

                if (HttpContext.Session.TryGetValue("security", out securityTemp))
                {
                    security = Encoding.ASCII.GetString(securityTemp);
                }

                var assessmentId = _token.AssessmentForUser();
                _crr.InstantiateScoringHelper(assessmentId);
                var model = GetCrrModel(assessmentId);
                var pageList = ReportHelper.GetReportList(view);
                List<PdfDocument> pdf = new List<PdfDocument>();
                PdfDocument tempPdf = null;
                int pageCount = 1;
                string baseUrl = UrlStringHelper.GetBaseUrl(Request);

                foreach (var page in pageList)
                {
                    var html = await ReportHelper.RenderRazorViewToString(this, page, model, baseUrl, _engine);
                    tempPdf = ReportHelper.RenderPdf(html, security, pageCount);

                    var title = page.ToLower();
                    title = _viewToTitle.ContainsKey(title) ? _viewToTitle[title] : page;
                    tempPdf.BookMarks.AddBookMarkAtStart(title, pageCount - 1);

                    pdf.Add(tempPdf);
                    pageCount = pageCount + tempPdf.PageCount;
                }

                var finalPdf = pdf.Count > 1 ? ReportHelper.MergePdf(pdf) : pdf.FirstOrDefault();
                return File(finalPdf.BinaryData, "application/pdf", ReportHelper.GetReportName(view));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult CrrReport(string token, string security)
        {
            if (_token.IsTokenValid(token))
            {
              
               return View(CrrHtmlInit(token, security));
            }

            return Unauthorized();
        }

        [HttpGet]
        public IActionResult CrrDeficiencyReport(string token, string security)
        {
            if (_token.IsTokenValid(token))
            {
                return View(CrrHtmlInit(token, security));
            }

            return Unauthorized();
        }

        [HttpGet]
        public IActionResult CrrCommentsMarked(string token, string security)
        {
            if (_token.IsTokenValid(token))
            {
                return View(CrrHtmlInit(token, security));
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("api/report/getCrrModel")]
        public IActionResult GetCrrModel()
        {
            var assessmentId = _token.AssessmentForUser();
            var crrModel = GetCrrModel(assessmentId);
            return Ok(crrModel);
        }

        private object CrrHtmlInit(string token, string security)
        {
            _token.Init(token);
            Request.Headers.Add("Authorization", token);
            var assessmentId = _token.AssessmentForUser();
            //_crr.InstantiateScoringHelper(assessmentId);
            HttpContext.Session.Set("token", Encoding.ASCII.GetBytes(token));
            HttpContext.Session.Set("security", Encoding.ASCII.GetBytes(security));
            return GetCrrModel(assessmentId);
        }

        private object GetCrrModel(int assessmentId, string token = "")
        {
            _crr.InstantiateScoringHelper(assessmentId);
            var detail = _assessment.GetAssessmentDetail(assessmentId, token);

            var demographics = _demographic.GetDemographics(assessmentId);

            //Testing
            _report.SetReportsAssessmentId(assessmentId);

            var deficiencyData = new MaturityBasicReportData()
            {
                Information = _report.GetInformation(),
                DeficienciesList = _report.GetMaturityDeficiencies(),
                Comments = _report.GetCommentsList(),
                MarkedForReviewList = _report.GetMarkedForReviewList(),
                QuestionsList = _report.GetQuestionsList()
            };
            CrrResultsModel crrResultsData = _crr.GetCrrResultsSummary(); //GenerateCrrResults();
            CrrViewModel viewModel = new CrrViewModel(detail, demographics.CriticalService, _crr, deficiencyData);
            viewModel.ReportChart = _crr.GetPercentageOfPractice();
            viewModel.crrResultsData = crrResultsData;
            return viewModel;
        }

        [HttpGet]
        [Route("api/report/getCrrHtml")]
        public async Task<IActionResult> GetCrrHtml(string view)
        {
            var assessmentId = _token.AssessmentForUser();
            _crr.InstantiateScoringHelper(assessmentId);
            var model = GetCrrModel(assessmentId);
            string baseUrl = UrlStringHelper.GetBaseUrl(Request);
            var html = new CrrHtml();
            html.Html = await ReportHelper.RenderRazorViewToString(this, view, model, baseUrl, _engine);

            return Ok(html);
        }

        /// <summary>
        /// Generates and returns markup (SVG) for a MIL
        /// heatmap widget.  
        /// </summary>
        /// <param name="mil"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/report/widget/milheatmap")]
        public IActionResult GetWidget([FromQuery] string domain, [FromQuery] string mil, [FromQuery] double? scale = null)
        {
            var assessmentId = _token.AssessmentForUser();
            _crr.InstantiateScoringHelper(assessmentId);


            var xMil = _crr.XDoc.XPathSelectElement($"//Domain[@abbreviation='{domain}']/Mil[@label='{mil}']");
            if (xMil == null)
            {
                return NotFound();
            }

            // populate the widget with the MIL strip and collapse any hidden goal strips
            var heatmap = new Helpers.ReportWidgets.MilHeatMap(xMil, true, true);
            if (scale != null)
            {
                heatmap.Scale((double)scale);
            }

            // return the svg
            return Content(heatmap.ToString(), "image/svg+xml");
        }

        private CrrResultsModel GenerateCrrResults()
        {
            MaturityReportData maturityData = new MaturityReportData(_context);

            maturityData.MaturityModels = _report.GetMaturityModelData();
            maturityData.information = _report.GetInformation();
            maturityData.AnalyzeMaturityData();


            // null out a few navigation properties to avoid circular references that blow up the JSON stringifier
            maturityData.MaturityModels.ForEach(d =>
            {
                d.MaturityQuestions.ForEach(q =>
                {
                    q.Answer.Assessment = null;
                });
            });

            CrrResultsModel retVal = new CrrResultsModel();
            List<DomainStats> cmmcDataDomainLevelStats = maturityData.MaturityModels.FirstOrDefault(d => d.MaturityModelName == "CRR")?.StatsByDomainAndLevel;
            retVal.EvaluateDataList(cmmcDataDomainLevelStats);
            retVal.TrimToNElements(10);
            retVal.GenerateWidthValues(); //If generating wrong values, check inner method values match the ones set in the css
            return retVal;
        }

        private CrrResultsModel generateCrrResults(MaturityReportData data)
        {
            //For Testing

            CrrResultsModel retVal = new CrrResultsModel();
            List<DomainStats> cmmcDataDomainLevelStats = data.MaturityModels.Where(d => d.MaturityModelName == "CRR").First().StatsByDomainAndLevel;
            retVal.EvaluateDataList(cmmcDataDomainLevelStats);
            retVal.TrimToNElements(10);
            retVal.GenerateWidthValues(); //If generating wrong values, check inner method values match the ones set in the css
            return retVal;
        }
    }

    public class CrrHtml
    {
        public string Html { get; set; }
    }
}