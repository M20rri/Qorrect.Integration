﻿using Microsoft.AspNetCore.Mvc;
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
        CourseDataAccessLayer courseDataAccessLayer = null;
        private readonly IConfiguration _configuration;
        public string QorrectBaseUrl { get; set; }

        public NavyController(IConfiguration configuration)
        {
            _configuration = configuration;
            courseDataAccessLayer = new CourseDataAccessLayer()
            {
                connectionString = _configuration.GetConnectionString("Constr")
            };
            QorrectBaseUrl = _configuration.GetValue<string>("QorrectBaseUrl");
        }

        [HttpPost]
        [Route("ImportCourseStandardFromBedo")]
        public async Task<IActionResult> ImportCourseStandardFromBedo([FromBody] DTOAddCourseRequest courseRequest)
        {

            string token = $"Bearer {courseRequest.BearerToken}";

            var bedoCourses = await courseDataAccessLayer.GetAllCourses();
            List<DTOAddEditCourse> addedCoursed = new List<DTOAddEditCourse>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponses = new List<DTOCognitiveLevelResponse>();

            foreach (var item in bedoCourses)
            {
                DTOAddEditCourse model = new DTOAddEditCourse()
                {
                    Name = item.CourseName,
                    Code = item.CourseCode,
                    CourseSubscriptionId = new Guid(courseRequest.CourseSubscriptionId),
                    CourseData = new DTOCourseData
                    {
                        CourseType = CourseType.Elective,
                        CreditHours = item.CreditHours == null ? 0 : item.CreditHours,
                        Description = item.Description,
                        LecturesHours = item.LectureHours,
                        PracticalHours = item.PracticalHours,
                        TotalHours = item.ClassesHours,
                        TotalMarks = item.TotalMarks
                    }
                };

                var client = new RestClient($"{QorrectBaseUrl}/courses");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", token);
                request.AddHeader("Content-Type", "application/json");

                request.AddParameter("application/json", JsonConvert.SerializeObject(model), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var result = JsonConvert.DeserializeObject<DTOAddEditCourse>(response.Content);
                if (result == null)
                {
                    return Ok(response.Content);
                }
                addedCoursed.Add(result);
            }

            foreach (var item in addedCoursed)
            {

                #region Get Course Cognitive Levels


                {
                    if (item.Id != null)
                    {
                        var client = new RestClient($"{QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={item.Id}");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("accept", "*/*");
                        request.AddHeader("Authorization", token);
                        IRestResponse response = client.Execute(request);
                        cognitiveLevelResponses = JsonConvert.DeserializeObject<List<DTOCognitiveLevelResponse>>(response.Content).ToList();


                        #region Delete Course Cognitive Levels

                        {
                            foreach (var cognitiveLevelResponse in cognitiveLevelResponses)
                            {

                                var clientCL = new RestClient($"{QorrectBaseUrl}/cognitivelevel/{cognitiveLevelResponse.Id}");
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


                #region Apply Outline structure to course

                {
                    var client = new RestClient($"{QorrectBaseUrl}/course/applyOutline");
                    client.Timeout = -1;
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Authorization", token);
                    request.AddHeader("Content-Type", "application/json");

                    var body = new DTOApplyOutlineStructure
                    {
                        Id = item.Id.Value
                    };

                    request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                }
                #endregion

            }

            return Ok(addedCoursed);
        }

        [HttpPost]
        [Route("ImportCourseStandardFromBedoLeaf")]
        public async Task<IActionResult> ImportCourseStandardFromBedoLeaf([FromBody] DTOAddCourseLevelRequest courseRequest)
        {
            string token = $"Bearer {courseRequest.BearerToken}";
            List<CourseLeaf> addedCourseLevels = new List<CourseLeaf>();
            DTOAddEditNodeLevel unitResponse = new DTOAddEditNodeLevel();
            List<DTOBedoCongnitiveLevel> congnitiveLevels = new List<DTOBedoCongnitiveLevel>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponse = new List<DTOCognitiveLevelResponse>();
            List<DTOBedoILO> bedoIlos = new List<DTOBedoILO>();
            List<DTOItemFromBedoByIloResponse> BedoQueastionsWithAnswers = new List<DTOItemFromBedoByIloResponse>();

            var bedoCourseLevels = await courseDataAccessLayer.GetCourseLevels(courseRequest.CourseId);
            congnitiveLevels = await courseDataAccessLayer.GetCongitive(courseRequest.CourseId);

            #region Add Bedo Cognitive Level in Qorrect

            foreach (var item in congnitiveLevels)
            {
                var client = new RestClient($"{QorrectBaseUrl}/cognitivelevel");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", token);
                request.AddHeader("Content-Type", "application/json");
                var body = new DTOAddCourseCognitiveLevelRequest
                {
                    Name = item.Name,
                    Code = item.Code,
                    CourseId = courseRequest.ParentId
                };
                request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                if (response.Content is null)
                {
                    return Ok(response.Content);
                }
            }

            #endregion

            #region Get Course Cognitive Levels

            {
                var client = new RestClient($"{QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={courseRequest.ParentId}");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("accept", "*/*");
                request.AddHeader("Authorization", token);
                IRestResponse response = client.Execute(request);
                cognitiveLevelResponse = JsonConvert.DeserializeObject<List<DTOCognitiveLevelResponse>>(response.Content).ToList();
                if (cognitiveLevelResponse == null)
                {
                    return Ok(response.Content);
                }
            }

            #endregion


            foreach (var item in bedoCourseLevels)
            {

                #region Add Node level authorized by teacher

                {
                    var client = new RestClient($"{QorrectBaseUrl}/courses/node");
                    client.Timeout = -1;
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Authorization", token);
                    request.AddHeader("Content-Type", "application/json");
                    var body = new DTOAddEditNodeLevel
                    {
                        Code = item.Code,
                        Name = item.Name,
                        Order = item.Order,
                        ParentId = courseRequest.ParentId
                    };

                    request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    unitResponse = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(response.Content);
                    if (unitResponse is null)
                    {
                        return Ok(response.Content);
                    }
                }

                #endregion

                #region Add Leaf Level to course outline

                {
                    List<Guid> ListOfIlOsInserted = new List<Guid>();

                    foreach (var node in item.Lessons)
                    {

                        DTOQorrectILORequest resultILO = new DTOQorrectILORequest();

                        #region Get Ilos from Bedo

                        {
                            bedoIlos = await courseDataAccessLayer.GetLevelIlo(node.Id);
                            foreach (var bedoIlo in bedoIlos)
                            {

                                {
                                    var clientILO = new RestClient($"{QorrectBaseUrl}/intendedlearningoutcome");
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
                                        CourseId = courseRequest.ParentId
                                    };
                                    requestILO.AddParameter("application/json", JsonConvert.SerializeObject(bodyILO), ParameterType.RequestBody);
                                    IRestResponse responseILO = clientILO.Execute(requestILO);
                                    resultILO = JsonConvert.DeserializeObject<DTOQorrectILORequest>(responseILO.Content);
                                    if (resultILO is null)
                                    {
                                        return Ok(responseILO.Content);
                                    }
                                    ListOfIlOsInserted.Add(Guid.Parse(resultILO.Id.ToString()));
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

                                var client = new RestClient($"{QorrectBaseUrl}/courses/leaf");
                                client.Timeout = -1;
                                var request = new RestRequest(Method.POST);
                                request.AddHeader("Authorization", token);
                                request.AddHeader("Content-Type", "application/json");
                                request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                                IRestResponse response = client.Execute(request);

                                resultleaf = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(response.Content);
                                if (resultleaf is null)
                                {
                                    return Ok(response.Content);
                                }

                            }

                            #region Get Questions from bedo by Ilo

                            {
                                foreach (var bedoIlo in bedoIlos)
                                {

                                    BedoQueastionsWithAnswers = await courseDataAccessLayer.GetItemsByIlo(bedoIlo.Id);
                                    foreach (var question in BedoQueastionsWithAnswers)
                                    {
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


                                        var client = new RestClient($"{QorrectBaseUrl}/item/mcq");
                                        client.Timeout = -1;
                                        var request = new RestRequest(Method.POST);
                                        request.AddHeader("Authorization", token);
                                        request.AddHeader("Content-Type", "application/json");
                                        var body = new DTOAddQuestion
                                        {
                                            CourseSubscriptionId = courseRequest.CourseSubscriptionId,
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
                                                Tags = new List<Guid?>(),
                                                ItemMappings = new List<DTOItemMapping>
                                                {
                                                    new DTOItemMapping
                                                    {
                                                        IloId = Guid.Parse(resultILO.Id.ToString()),
                                                        LevelId =  resultleaf.Id
                                                    }
                                                }
                                            },
                                            TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // Please don't forget to change

                                        };
                                        request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                                        IRestResponse response = client.Execute(request);

                                    }

                                }

                            }

                            #endregion
                        }

                        #endregion

                    }

                }

                #endregion
            }
            return Ok();
        }

        [HttpPost]
        [Route("ImportAllFromBedo")]
        public async Task<IActionResult> ImportAllFromBedo([FromBody] DTOAddCourseRequest courseRequest)
        {

            string token = $"Bearer {courseRequest.BearerToken}";

            var bedoCourses = await courseDataAccessLayer.GetAllCourses();
            List<DTOAddEditCourse> addedCoursed = new List<DTOAddEditCourse>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponses = new List<DTOCognitiveLevelResponse>();


            List<CourseLeaf> addedCourseLevels = new List<CourseLeaf>();
            DTOAddEditNodeLevel unitResponse = new DTOAddEditNodeLevel();
            List<DTOBedoCongnitiveLevel> congnitiveLevels = new List<DTOBedoCongnitiveLevel>();
            List<DTOCognitiveLevelResponse> cognitiveLevelResponse = new List<DTOCognitiveLevelResponse>();
            List<DTOBedoILO> bedoIlos = new List<DTOBedoILO>();
            List<DTOItemFromBedoByIloResponse> BedoQueastionsWithAnswers = new List<DTOItemFromBedoByIloResponse>();



            foreach (var bedoCourseitem in bedoCourses)
            {
                var bedoCourseLevels = await courseDataAccessLayer.GetCourseLevels(bedoCourseitem.Id);
                congnitiveLevels = await courseDataAccessLayer.GetCongitive(bedoCourseitem.Id);
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
                        TotalMarks = bedoCourseitem.TotalMarks
                    }
                };

                var client = new RestClient($"{QorrectBaseUrl}/courses");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", token);
                request.AddHeader("Content-Type", "application/json");

                request.AddParameter("application/json", JsonConvert.SerializeObject(model), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var item = JsonConvert.DeserializeObject<DTOAddEditCourse>(response.Content);
                //    if (item == null)
                //    {
                //        return Ok(response.Content);
                //    }
                //    addedCoursed.Add(item);
                //}

                //foreach (var item in addedCoursed)
                //{

                #region Apply Outline structure to course

                {
                    var applyOutlineclient = new RestClient($"{QorrectBaseUrl}/course/applyOutline");
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
                        var cognitivelevelsclient = new RestClient($"{QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={item.Id}");
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

                                var clientCL = new RestClient($"{QorrectBaseUrl}/cognitivelevel/{cognitiveLevelResponseToDelete.Id}");
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
                        var qclclient = new RestClient($"{QorrectBaseUrl}/cognitivelevel");
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
                        //if (response.Content is null)
                        //{
                        //    return Ok(response.Content);
                        //}
                    }

                    #endregion

                    #region Get Course Cognitive Levels

                    {
                        var bclclient = new RestClient($"{QorrectBaseUrl}/cognitivelevels?page=1&pageSize=10&courseId={ParentId}");
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


                    foreach (var bedoCourseLevelitem in bedoCourseLevels)
                    {

                        #region Add Node level authorized by teacher

                        {
                            var nodeclient = new RestClient($"{QorrectBaseUrl}/courses/node");
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
                            List<Guid> ListOfIlOsInserted = new List<Guid>();

                            foreach (var node in bedoCourseLevelitem.Lessons)
                            {

                                DTOQorrectILORequest resultILO = new DTOQorrectILORequest();

                                #region Get Ilos from Bedo

                                {
                                    bedoIlos = await courseDataAccessLayer.GetLevelIlo(node.Id);
                                    foreach (var bedoIlo in bedoIlos)
                                    {

                                        {
                                            var clientILO = new RestClient($"{QorrectBaseUrl}/intendedlearningoutcome");
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
                                            ListOfIlOsInserted.Add(Guid.Parse(resultILO.Id.ToString()));
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

                                        var leafclient = new RestClient($"{QorrectBaseUrl}/courses/leaf");
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

                                            BedoQueastionsWithAnswers = await courseDataAccessLayer.GetItemsByIlo(bedoIlo.Id);
                                            foreach (var question in BedoQueastionsWithAnswers)
                                            {
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
                                                var mcqclient = new RestClient($"{QorrectBaseUrl}/item/mcq");
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
                                                        Tags = new List<Guid?>(),
                                                        ItemMappings = new List<DTOItemMapping>
                                                {
                                                    new DTOItemMapping
                                                    {
                                                        IloId = Guid.Parse(resultILO.Id.ToString()),
                                                        LevelId =  resultleaf.Id
                                                    }
                                                }
                                                    },
                                                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will chamge it

                                                };
                                                mcqrequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                                                IRestResponse mcqresponse = mcqclient.Execute(mcqrequest);

                                            }

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

            return Ok(addedCoursed);
        }
    }
}
