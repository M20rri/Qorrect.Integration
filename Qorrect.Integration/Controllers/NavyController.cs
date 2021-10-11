using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Qorrect.Integration.Models;
using Qorrect.Integration.Services;
using RestSharp;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Qorrect.Integration.Helper;


namespace Qorrect.Integration.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class NavyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        DTOManageUrl _configUrl = null;
        public string bedoIntegrationString { get; set; }

        public NavyController(IConfiguration configuration)
        {
            _configuration = configuration;
            bedoIntegrationString = _configuration.GetConnectionString("BedoIntegrateConstr");
            _configUrl = new CourseDataAccessLayer().GetMoodleBaseUrl(bedoIntegrationString).Result;
        }

        [HttpGet]
        [Route("BedoCousresList")]
        public async Task<IActionResult> BedoCousresList()
        {
            var bedoCourses = await new CourseDataAccessLayer().GetAllCourses(_configUrl.BedobaseUrl);
            return Ok(bedoCourses);
        }
        [HttpGet]
        [Route("GetLostItems/{id}")]
        public async Task<IActionResult> GetLostItems([FromRoute] string id)
        {
            var bedoCourses = await new CourseDataAccessLayer().GetLostItems(bedoIntegrationString,id);
            return Ok(bedoCourses);
        }
        [HttpGet]
        [Route("TransferedCoursesList")]
        public async Task<IActionResult> GetTransferedCoursesList()
        {
            var bedoCourses = await new CourseDataAccessLayer().GetTransferedCourses(bedoIntegrationString);
            return Ok(bedoCourses);
        }
        [HttpGet]
        [Route("QorrectModules/{id}")]
        public async Task<IActionResult> QorrectModules([FromRoute] string id)
        {
            string token = $"Bearer {id}";
            var client = new RestClient($"{_configUrl.QorrectBaseUrl}/coursesubscription?page=1&pageSize=30&isArchived=false");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Accept", "application/json, text/plain, */*");
            request.AddHeader("Authorization", token);
            request.AddHeader("Accept-Language", "en-US");
            IRestResponse response = await client.ExecuteAsync(request);
            return Ok(JsonConvert.DeserializeObject<DTOQorrectModulesResponse>(response.Content));
        }

        [HttpPost]
        [Route("ImportAllFromBedo")]
        public async Task<IActionResult> ImportAllFromBedo([FromBody] DTOAddCourseRequest courseRequest)
        {
            string token = $"Bearer {courseRequest.BearerToken}";

            string TagSearchID = "";

            #region Question Tags

            {
                var tagClient = new RestClient($"{_configUrl.QorrectBaseUrl}/tags?page=1&pageSize=10&searchText=FromBedo");
                tagClient.Timeout = -1;
                var tagRequest = new RestRequest(Method.GET);
                tagRequest.AddHeader("Connection", "keep-alive");
                tagRequest.AddHeader("sec-ch-ua", "\"Google Chrome\";v=\"93\", \" Not;A Brand\";v=\"99\", \"Chromium\";v=\"93\"");
                tagRequest.AddHeader("Accept", "application/json, text/plain, */*");
                tagRequest.AddHeader("Authorization", token);
                tagRequest.AddHeader("Accept-Language", "en-US");
                tagRequest.AddHeader("sec-ch-ua-mobile", "?0");
                tagClient.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36";
                tagRequest.AddHeader("sec-ch-ua-platform", "\"Windows\"");
                tagRequest.AddHeader("Origin", "http://localhost:4200");
                tagRequest.AddHeader("Sec-Fetch-Site", "same-site");
                tagRequest.AddHeader("Sec-Fetch-Mode", "cors");
                tagRequest.AddHeader("Sec-Fetch-Dest", "empty");
                tagRequest.AddHeader("Referer", "http://localhost:4200/");
                IRestResponse tagResponse = tagClient.Execute(tagRequest);
                List<DTOTag> tags = JsonConvert.DeserializeObject<List<DTOTag>>(tagResponse.Content);
                TagSearchID = tags.Any() ? JsonConvert.DeserializeObject<List<DTOTag>>(tagResponse.Content).FirstOrDefault().id : null;
            }
            List<DTOTag> _Tags = new List<DTOTag>() { new DTOTag { id = TagSearchID.ToString(), name = "FromBedo" } };
            #endregion


            List<DTOAddEditCourse> addedCoursed = new List<DTOAddEditCourse>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponses = new List<DTOCognitiveLevelResponse>();

            List<CourseLeaf> addedCourseLevels = new List<CourseLeaf>();
            DTOAddEditNodeLevel unitResponse = new DTOAddEditNodeLevel();
            List<DTOBedoCongnitiveLevel> congnitiveLevels = new List<DTOBedoCongnitiveLevel>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponse = new List<DTOCognitiveLevelResponse>();
            List<DTOBedoILO> bedoIlos = new List<DTOBedoILO>();
            List<DTOItemFromBedoByIloResponse> BedoQueastionsWithAnswers = new List<DTOItemFromBedoByIloResponse>();

            foreach (var bedoCourseitem in courseRequest.Courses)
            {
                int BedoCourseId = bedoCourseitem.Id;

                var bedoCourseLevels = await new CourseDataAccessLayer().GetCourseLevels(_configUrl.BedobaseUrl , bedoCourseitem.Id);
                congnitiveLevels = await new CourseDataAccessLayer().GetCongitive(_configUrl.BedobaseUrl , bedoCourseitem.Id);
                DTOAddEditCourse model = new DTOAddEditCourse()
                {
                    Name = bedoCourseitem.CourseName,
                    Code = bedoCourseitem.CourseCode,
                    CourseSubscriptionId = new Guid(courseRequest.CourseSubscriptionId),
                    CourseData = new DTOCourseData
                    {
                        CourseType = CourseType.Elective,
                        CreditHours = bedoCourseitem.CreditHours == null ? 0 : bedoCourseitem.CreditHours,
                        Description = bedoCourseitem.Description,
                        LecturesHours = bedoCourseitem.LectureHours,
                        PracticalHours = bedoCourseitem.PracticalHours,
                        TotalHours = bedoCourseitem.ClassesHours,
                        TotalMarks = bedoCourseitem.TotalMarks,
                        Tags = _Tags
                    }
                };

                var client = new RestClient($"{_configUrl.QorrectBaseUrl}/courses");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", token);
                request.AddHeader("Content-Type", "application/json");

                request.AddParameter("application/json", JsonConvert.SerializeObject(model), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var item = JsonConvert.DeserializeObject<DTOAddEditCourse>(response.Content);
                #region Apply Outline structure to course
                {
                    var applyOutlineclient = new RestClient($"{_configUrl.QorrectBaseUrl}/course/applyOutline");
                    applyOutlineclient.Timeout = -1;
                    var applyOutlinerequest = new RestRequest(Method.POST);
                    applyOutlinerequest.AddHeader("Authorization", token);
                    applyOutlinerequest.AddHeader("Content-Type", "application/json");

                    var body = new DTOApplyOutlineStructure
                    {
                        Id = item.Id.Value
                    };

                    applyOutlinerequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                    IRestResponse applyOutlineresponse = applyOutlineclient.Execute(applyOutlinerequest);
                }
                #endregion

                #region Get Course Cognitive Levels


                {
                    if (item.Id != null)
                    {
                        var cognitivelevelsclient = new RestClient($"{_configUrl.QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={item.Id}");
                        cognitivelevelsclient.Timeout = -1;
                        var cognitivelevelsrequest = new RestRequest(Method.GET);
                        cognitivelevelsrequest.AddHeader("accept", "*/*");
                        cognitivelevelsrequest.AddHeader("Authorization", token);
                        IRestResponse clresponse = cognitivelevelsclient.Execute(cognitivelevelsrequest);
                        cognitiveLevelResponses = JsonConvert.DeserializeObject<List<DTOCognitiveLevelResponse>>(clresponse.Content).ToList();


                        #region Delete Course Cognitive Levels

                        {
                            foreach (var cognitiveLevelResponseToDelete in cognitiveLevelResponses)
                            {

                                var clientCL = new RestClient($"{_configUrl.QorrectBaseUrl}/cognitivelevel/{cognitiveLevelResponseToDelete.Id}");
                                clientCL.Timeout = -1;
                                var requestCL = new RestRequest(Method.DELETE);
                                requestCL.AddHeader("accept", "*/*");
                                requestCL.AddHeader("Authorization", token);
                                IRestResponse responseCL = clientCL.Execute(requestCL);
                            }

                        }

                        #endregion
                    }

                }


                #endregion
                Guid ParentId = Guid.Parse(item.Id.ToString());
                #region AddCourseLevelsMCQs
                {
                    #region Add Bedo Cognitive Level in Qorrect

                    foreach (var congnitiveLevelItem in congnitiveLevels)
                    {
                        var qclclient = new RestClient($"{_configUrl.QorrectBaseUrl}/cognitivelevel");
                        qclclient.Timeout = -1;
                        var qclrequest = new RestRequest(Method.POST);
                        qclrequest.AddHeader("Authorization", token);
                        qclrequest.AddHeader("Content-Type", "application/json");
                        var body = new DTOAddCourseCognitiveLevelRequest
                        {
                            Name = congnitiveLevelItem.Name,
                            Code = congnitiveLevelItem.Code,
                            CourseId = ParentId
                        };
                        qclrequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                        IRestResponse qclresponse = qclclient.Execute(qclrequest);
                    }

                    #endregion

                    #region Get Course Cognitive Levels

                    {
                        var bclclient = new RestClient($"{_configUrl.QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={ParentId}");
                        bclclient.Timeout = -1;
                        var bclrequest = new RestRequest(Method.GET);
                        bclrequest.AddHeader("accept", "*/*");
                        bclrequest.AddHeader("Authorization", token);
                        IRestResponse bclresponse = bclclient.Execute(bclrequest);
                        cognitiveLevelResponse = JsonConvert.DeserializeObject<List<DTOCognitiveLevelResponse>>(bclresponse.Content).ToList();
                        if (cognitiveLevelResponse == null)
                        {
                            return Ok(response.Content);
                        }
                    }

                    #endregion

                    List<int> ListOfBedoIlosInsertedtoQorrect = new List<int>();
                    List<int> ListOfBedoItemsInsertedtoQorrect = new List<int>();
                    foreach (var bedoCourseLevelitem in bedoCourseLevels)
                    {

                        #region Add Node level authorized by teacher

                        {
                            var nodeclient = new RestClient($"{_configUrl.QorrectBaseUrl}/courses/node");
                            nodeclient.Timeout = -1;
                            var noderequest = new RestRequest(Method.POST);
                            noderequest.AddHeader("Authorization", token);
                            noderequest.AddHeader("Content-Type", "application/json");
                            var body = new DTOAddEditNodeLevel
                            {
                                Code = bedoCourseLevelitem.Code,
                                Name = bedoCourseLevelitem.Name,
                                Order = bedoCourseLevelitem.Order,
                                ParentId = ParentId
                            };

                            noderequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                            IRestResponse noderesponse = nodeclient.Execute(noderequest);
                            unitResponse = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(noderesponse.Content);
                            if (unitResponse is null)
                            {
                                return Ok(noderesponse.Content);
                            }
                        }

                        #endregion

                        #region Add Leaf Level to course outline

                        {



                            foreach (var node in bedoCourseLevelitem.Lessons)
                            {

                                List<Guid> ListOfIlOsInserted = new List<Guid>();
                                DTOQorrectILORequest resultILO = new DTOQorrectILORequest();

                                #region Get Ilos from Bedo
                                {
                                    bedoIlos = await new CourseDataAccessLayer().GetLevelIlo(_configUrl.BedobaseUrl , node.Id);
                                    foreach (var bedoIlo in bedoIlos)
                                    {
                                        {
                                            var clientILO = new RestClient($"{_configUrl.QorrectBaseUrl}/intendedlearningoutcome");
                                            clientILO.Timeout = -1;
                                            var requestILO = new RestRequest(Method.POST);
                                            requestILO.AddHeader("Authorization", token);
                                            requestILO.AddHeader("Content-Type", "application/json");
                                            var bodyILO = new DTOQorrectILORequest
                                            {
                                                Name = bedoIlo.Name,
                                                Code = bedoIlo.Code,
                                                CourseCognitiveLevelId = cognitiveLevelResponse.FirstOrDefault(a => a.Name.Equals(bedoIlo.CognitiveName)).Id,
                                                CourseCognitiveLevelName = cognitiveLevelResponse.FirstOrDefault(a => a.Name.Equals(bedoIlo.CognitiveName)).Name,
                                                CourseId = ParentId
                                            };
                                            requestILO.AddParameter("application/json", JsonConvert.SerializeObject(bodyILO), ParameterType.RequestBody);
                                            IRestResponse responseILO = clientILO.Execute(requestILO);
                                            resultILO = JsonConvert.DeserializeObject<DTOQorrectILORequest>(responseILO.Content);
                                            if (resultILO is null)
                                            {
                                                return Ok(responseILO.Content);
                                            }
                                            {
                                                ListOfIlOsInserted.Add(Guid.Parse(resultILO.Id.ToString()));
                                            }
                                        }

                                    }



                                }

                                #endregion

                                #region Add Lesson

                                DTOAddEditNodeLevel resultleaf = new DTOAddEditNodeLevel();

                                {
                                    {
                                        var body = new CourseLeaf
                                        {
                                            Code = node.Code,
                                            Name = node.Name,
                                            Order = node.Order,
                                            ParentId = unitResponse.Id.Value,
                                            IntendedLearningOutcomes = ListOfIlOsInserted
                                        };

                                        var leafclient = new RestClient($"{_configUrl.QorrectBaseUrl}/courses/leaf");
                                        leafclient.Timeout = -1;
                                        var leafrequest = new RestRequest(Method.POST);
                                        leafrequest.AddHeader("Authorization", token);
                                        leafrequest.AddHeader("Content-Type", "application/json");
                                        leafrequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                                        IRestResponse leafresponse = leafclient.Execute(leafrequest);

                                        resultleaf = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(leafresponse.Content);
                                        if (resultleaf is null)
                                        {
                                            return Ok(leafresponse.Content);
                                        }

                                    }
                                    #region Get Questions from bedo by Ilo

                                    {

                                        foreach (var bedoIlo in bedoIlos)
                                        {

                                            BedoQueastionsWithAnswers = await new CourseDataAccessLayer().GetItemsByIlo(_configUrl.BedobaseUrl , bedoIlo.Id);
                                            //if (BedoQueastionsWithAnswers. == 18777)
                                            //{
                                            //    var zzz = bedoIlo;
                                            //    int cc = questionEssay.Id;
                                            //}
                                            #region MCQ
                                            foreach (var question in BedoQueastionsWithAnswers.Where(x => x.QuestionTypeID == 1))
                                            {
                                                if (ListOfBedoItemsInsertedtoQorrect.Contains(question.Id)) { continue; }
                                                ListOfBedoItemsInsertedtoQorrect.Add(question.Id);
                                                List<DTOAnswer> dTOAnswers = new List<DTOAnswer>();
                                                foreach (var answer in question.Answers)
                                                {
                                                    dTOAnswers.Add(new DTOAnswer
                                                    {
                                                        Text = answer.Answer,
                                                        PlainText = answer.Answer,
                                                        IsCorrect = answer.TrueFalse
                                                    });
                                                }

                                                Guid CourseSubscriptionId = Guid.Parse(courseRequest.CourseSubscriptionId);
                                                var mcqclient = new RestClient($"{_configUrl.QorrectBaseUrl}/item/mcq");
                                                mcqclient.Timeout = -1;
                                                var mcqrequest = new RestRequest(Method.POST);
                                                mcqrequest.AddHeader("Authorization", token);
                                                mcqrequest.AddHeader("Content-Type", "application/json");
                                                var body = new DTOAddQuestion
                                                {
                                                    CourseSubscriptionId = CourseSubscriptionId,
                                                    Version = new DTOVersion
                                                    {
                                                        Stem = new DTOStem
                                                        {
                                                            Text = question.Stem,
                                                            PlainText = question.Stem,
                                                            Comment = "no",
                                                            Difficulty = 0,
                                                            Settings = new DTOSettings
                                                            {
                                                                IsShuffleAnswers = true,
                                                                IsAllowForTrialExams = true,
                                                                Difficulty = 1,
                                                                ExpectedTime = 1,
                                                                IsAllowedForComputerBasedOnly = true
                                                            },
                                                            Answers = dTOAnswers
                                                        },
                                                        ItemClassification = 1,
                                                        Tags = _Tags,
                                                        ItemMappings = new List<DTOItemMapping>
                                                {
                                                    new DTOItemMapping
                                                    {
                                                        IloId = Guid.Parse(resultILO.Id.ToString()),
                                                        LevelId =  resultleaf.Id
                                                    }
                                                }
                                                    },
                                                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will change it

                                                };
                                                mcqrequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                                                IRestResponse mcqresponse = mcqclient.Execute(mcqrequest);

                                                #region Log in Database

                                                {
                                                    var logger = new DTORequestResponseLog
                                                    {
                                                        CourseID = BedoCourseId,
                                                        CourseName = bedoCourseitem.CourseName,
                                                        Device = "Bedo",
                                                        ErrorQuestionID = question.Id,
                                                        logRequest = JsonConvert.SerializeObject(body),
                                                        logResponse = JsonConvert.SerializeObject(mcqresponse.Content),
                                                        RequestUri = mcqclient.BaseUrl.AbsoluteUri,
                                                        StatusCode = mcqresponse.StatusDescription,
                                                        QuestionID = question.Id
                                                    };

                                                    await new CourseDataAccessLayer().RequestResponseLogger(bedoIntegrationString , logger);

                                                }

                                                #endregion

                                            }
                                            #endregion
                                            #region Essay
                                            foreach (var questionEssay in BedoQueastionsWithAnswers.Where(x => x.QuestionTypeID == 4))
                                            {
                                                if (ListOfBedoItemsInsertedtoQorrect.Contains(questionEssay.Id)) { continue; }
                                                ListOfBedoItemsInsertedtoQorrect.Add(questionEssay.Id);
                                                Guid CourseSubscriptionId = Guid.Parse(courseRequest.CourseSubscriptionId);
                                                var Essayclient = new RestClient($"{_configUrl.QorrectBaseUrl}/item/Essay");
                                                Essayclient.Timeout = -1;
                                                var Essayrequest = new RestRequest(Method.POST);
                                                Essayrequest.AddHeader("Authorization", token);
                                                Essayrequest.AddHeader("Content-Type", "application/json");
                                                var Essaybody = new DTOAddEssayQuestion
                                                {
                                                    CourseSubscriptionId = CourseSubscriptionId,
                                                    Version = new DTOEssayVersion
                                                    {

                                                        Stem = new DTOEssayStem
                                                        {
                                                            Direction = "FromBedo",
                                                            Text = questionEssay.Stem,
                                                            PlainText = questionEssay.Stem,
                                                            Answer = new DTOEssayAnswer
                                                            {
                                                                modelAnswer = "From Bedo",
                                                                modelAnswerPlainText = "From Bedo"

                                                            },
                                                            Comment = "no",
                                                            Difficulty = 0,
                                                            Settings = new DTOSettings
                                                            {
                                                                IsShuffleAnswers = true,
                                                                IsAllowForTrialExams = true,
                                                                Difficulty = 1,
                                                                ExpectedTime = 1,
                                                                IsAllowedForComputerBasedOnly = true
                                                            },
                                                            //  Answers = dTOAnswers
                                                        },
                                                        ItemClassification = 1,
                                                        Tags = _Tags,
                                                        ItemMappings = new List<DTOItemMapping>() {
                                                                  new DTOItemMapping{IloId = Guid.Parse(resultILO.Id.ToString()) , LevelId = resultleaf.Id }
                                                        }
                                                    },
                                                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will chamge it

                                                };
                                                Essayrequest.AddParameter("application/json", JsonConvert.SerializeObject(Essaybody), ParameterType.RequestBody);
                                                IRestResponse Essayresponse = Essayclient.Execute(Essayrequest);
                                                #region Log in Database

                                                {
                                                    var logger = new DTORequestResponseLog
                                                    {
                                                        CourseID = BedoCourseId,
                                                        CourseName = bedoCourseitem.CourseName,
                                                        Device = "Bedo",
                                                        ErrorQuestionID = questionEssay.Id,
                                                        logRequest = JsonConvert.SerializeObject(Essaybody),
                                                        logResponse = JsonConvert.SerializeObject(Essayresponse.Content),
                                                        RequestUri = Essayclient.BaseUrl.AbsoluteUri,
                                                        StatusCode = Essayresponse.StatusDescription,
                                                        QuestionID = questionEssay.Id
                                                    };

                                                    await new CourseDataAccessLayer().RequestResponseLogger(bedoIntegrationString, logger);

                                                }

                                                #endregion
                                            }
                                            #endregion


                                        }

                                    }

                                    #endregion
                                }

                                #endregion

                            }
                        }

                        #endregion
                    }
                }
                #endregion

            }

            #region Invisible Added bedo Cousres
            {
                string ids = string.Join(", ", courseRequest.Courses.Select(p => p.Id));
                var newCourses = await new CourseDataAccessLayer().InvisibleAddedCourses(_configUrl.BedobaseUrl , ids);
            }
            #endregion

            return Ok(addedCoursed);
        }
    }
}
