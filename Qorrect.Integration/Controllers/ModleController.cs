using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Qorrect.Integration.Helper;
using Qorrect.Integration.Models;
using RestSharp;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Qorrect.Integration.Services;

namespace Qorrect.Integration.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModleController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        DTOManageUrl _configUrl = null;
        public string bedoIntegrationString { get; set; }

        public ModleController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            bedoIntegrationString = _configuration.GetConnectionString("BedoIntegrateConstr");
            _configUrl = new CourseDataAccessLayer().GetMoodleBaseUrl(bedoIntegrationString).Result;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        [Route("GenerateToken")]
        public async Task<IActionResult> GenerateToken(DTOLogin model)
        {
            var client = new RestClient($"{_configUrl.MoodlebaseUrl}/login/token.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddParameter("username", model.username, ParameterType.QueryString);
            request.AddParameter("password", model.password, ParameterType.QueryString);
            request.AddParameter("service", "moodle_mobile_app", ParameterType.QueryString);
            IRestResponse response = await client.ExecuteAsync(request);
            return Ok(JsonConvert.DeserializeObject<DTOTokenResponse>(response.Content));
        }

        [HttpGet]
        [Route("CategoryList")]
        public async Task<IActionResult> CategoryList([FromQuery] string wstoken)
        {
            List<DTOMoodleCategory> moodleCategories = new List<DTOMoodleCategory>();
            var client = new RestClient($"{_configUrl.MoodlebaseUrl}/webservice/rest/server.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddParameter("wstoken", wstoken, ParameterType.QueryString);
            request.AddParameter("wsfunction", "core_course_get_categories", ParameterType.QueryString);
            request.AddParameter("moodlewsrestformat", "json", ParameterType.QueryString);
            IRestResponse response = await client.ExecuteAsync(request);
            moodleCategories = JsonConvert.DeserializeObject<List<DTOMoodleCategory>>(response.Content);
            return Ok(moodleCategories);
        }


        [HttpGet]
        [Route("CourseList")]
        public async Task<IActionResult> CourseList([FromQuery] string wstoken , [FromQuery] string codes)
        {
            List<string> _codes = codes.Split(",").ToList();
            List<DTOMoodleCategory> moodleCategories = new List<DTOMoodleCategory>();
            DTOModleCourse moodleCourse = new DTOModleCourse();
            List<Cours> courses = new List<Cours>();
            #region get Categories
            {
              
                var client = new RestClient($"{_configUrl.MoodlebaseUrl}/webservice/rest/server.php");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddParameter("wstoken", wstoken, ParameterType.QueryString);
                request.AddParameter("wsfunction", "core_course_get_categories", ParameterType.QueryString);
                request.AddParameter("moodlewsrestformat", "json", ParameterType.QueryString);
                IRestResponse response = await client.ExecuteAsync(request);
                moodleCategories = JsonConvert.DeserializeObject<List<DTOMoodleCategory>>(response.Content);
            }
            #endregion

            #region Get Courses
            {
                foreach (var item in moodleCategories)
                {
                    var client = new RestClient($"{_configUrl.MoodlebaseUrl}/webservice/rest/server.php");
                    client.Timeout = -1;
                    var request = new RestRequest(Method.GET);
                    request.AddParameter("wstoken", wstoken, ParameterType.QueryString);
                    request.AddParameter("wsfunction", "core_course_get_courses_by_field", ParameterType.QueryString);
                    request.AddParameter("field", "category", ParameterType.QueryString);
                    request.AddParameter("value", item.id, ParameterType.QueryString);
                    request.AddParameter("moodlewsrestformat", "json", ParameterType.QueryString);
                    IRestResponse courseReseponse = await client.ExecuteAsync(request);
                    var response = JsonConvert.DeserializeObject<DTOModleCourse>(courseReseponse.Content);
                    response.courses.Select(c => { c.category = item.name; return c; }).ToList();
                    courses.AddRange(response.courses.ToList());
                }

               
            }
            #endregion

            var filteredCourses = courses.Where(a => _codes.Contains(a.shortname)).ToList();

            moodleCourse.courses = filteredCourses;

            return Ok(moodleCourse);
        }


        [HttpPost]
        [Route("ImportAllFromModle")]
        public async Task<IActionResult> ImportAllFromModle([FromForm] DTOAddModleCourseRequest courseRequest)
        {
            string token = $"Bearer {courseRequest.BearerToken}";

            string TagSearchID = "";

            #region Question Tags

            {
                var tagClient = new RestClient($"{_configUrl.QorrectBaseUrl}/tags?page=1&pageSize=10&searchText=FromMoodle");
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
            List<DTOTag> _Tags = new List<DTOTag>() { new DTOTag { id = TagSearchID.ToString(), name = "FromMoodle" } };
            #endregion


            Cours Course = JsonConvert.DeserializeObject<Cours>(courseRequest.Course);
            string _Description = StripHTML(Course.summary);

            DTOAddEditCourse model = new DTOAddEditCourse()
            {
                Name = Course.fullname,
                Code = Course.shortname,
                CourseSubscriptionId = new Guid(courseRequest.CourseSubscriptionId),

                CourseData = new DTOCourseData
                {
                    CourseType = CourseType.Elective,
                    CreditHours = 100,
                    Description = _Description,
                    LecturesHours = 60,
                    PracticalHours = 20,
                    TotalHours = 80,
                    TotalMarks = 80,
                    Tags = _Tags
                }
            };


            var client = new RestClient($"{_configUrl.QorrectBaseUrl}/courses");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", token);
            request.AddHeader("Content-Type", "application/json");

            request.AddParameter("application/json", JsonConvert.SerializeObject(model), ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            var item = JsonConvert.DeserializeObject<DTOAddEditCourse>(response.Content);
            /////
            var clogger = new DTORequestResponseLog
            {
                CourseID = Convert.ToInt32(Course.id),
                CourseName = Course.fullname,
                Device = "Moodel",
                ErrorQuestionID = 0,
                logRequest = JsonConvert.SerializeObject(model),
                logResponse = JsonConvert.SerializeObject(response.Content),
                RequestUri = client.BaseUrl.AbsoluteUri,
                StatusCode = response.StatusDescription,
                QuestionID = 0
            };
            await new CourseDataAccessLayer().RequestResponseLogger(bedoIntegrationString, clogger);
            ////////
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
                IRestResponse applyOutlineresponse = await applyOutlineclient.ExecuteAsync(applyOutlinerequest);
            }
            #endregion
            Guid ParentId = Guid.Parse(item.Id.ToString());

            var Unitsclient = new RestClient($"{_configUrl.MoodlebaseUrl}/webservice/rest/server.php");
            Unitsclient.Timeout = -1;
            var Unitsrequest = new RestRequest(Method.GET);
            Unitsrequest.AddParameter("wstoken", courseRequest.ModleToken, ParameterType.QueryString);
            Unitsrequest.AddParameter("wsfunction", "core_course_get_contents", ParameterType.QueryString);
            Unitsrequest.AddParameter("courseid", Course.id, ParameterType.QueryString);
            Unitsrequest.AddParameter("moodlewsrestformat", "json", ParameterType.QueryString);
            IRestResponse Unitsresponse = await Unitsclient.ExecuteAsync(Unitsrequest);
            List<ModelUnit> ModelCourseLevels = JsonConvert.DeserializeObject<List<ModelUnit>>(Unitsresponse.Content);
            int unitOrder = 1;
            DTOAddEditNodeLevel unitResponse = new DTOAddEditNodeLevel();
            foreach (var bedoCourseLevelitem in ModelCourseLevels)
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
                        Code = bedoCourseLevelitem.name,
                        Name = bedoCourseLevelitem.name,
                        Order = unitOrder,
                        ParentId = ParentId
                    };

                    noderequest.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                    IRestResponse noderesponse = nodeclient.Execute(noderequest);
                    unitResponse = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(noderesponse.Content);
                    unitOrder = unitOrder++;
                    if (unitResponse is null)
                    {
                        return Ok(noderesponse.Content);
                    }



                    #region Add Leaf Level to course outline

                    {
                        List<Guid> ListOfIlOsInserted = new List<Guid>();
                        int LessonOrder = 1;
                        foreach (var node in bedoCourseLevelitem.modules)
                        {

                            DTOQorrectILORequest resultILO = new DTOQorrectILORequest();

                            #region Add Lesson

                            DTOAddEditNodeLevel resultleaf = new DTOAddEditNodeLevel();

                            {
                                {
                                    var Lessonbody = new CourseLeaf
                                    {
                                        Code = node.name,
                                        Name = node.name,
                                        Order = LessonOrder,
                                        ParentId = unitResponse.Id.Value
                                    };

                                    var leafclient = new RestClient($"{_configUrl.QorrectBaseUrl}/courses/leaf");
                                    leafclient.Timeout = -1;
                                    var leafrequest = new RestRequest(Method.POST);
                                    leafrequest.AddHeader("Authorization", token);
                                    leafrequest.AddHeader("Content-Type", "application/json");
                                    leafrequest.AddParameter("application/json", JsonConvert.SerializeObject(Lessonbody), ParameterType.RequestBody);
                                    IRestResponse leafresponse = leafclient.Execute(leafrequest);

                                    resultleaf = JsonConvert.DeserializeObject<DTOAddEditNodeLevel>(leafresponse.Content);
                                    if (resultleaf is null)
                                    {
                                        return Ok(leafresponse.Content);
                                    }
                                    LessonOrder = LessonOrder++;
                                }

                            }

                            #endregion
                            LessonOrder = 1;
                        }

                    }

                    #endregion
                }

                #endregion
            }

            #region Get Questions from XML

            {
                Model mediaResponse = new Model();

                var path = Path.Combine(Directory.GetCurrentDirectory(), "DataSource", courseRequest.XMLFile.FileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await courseRequest.XMLFile.CopyToAsync(stream);
                }


                {
                    string xmlfile = Path.Combine(Directory.GetCurrentDirectory(), "DataSource", courseRequest.XMLFile.FileName);
                    XDocument xmlDoc = XDocument.Load(xmlfile);
                    IEnumerable<XElement> quizes = xmlDoc.Descendants("question");
                    foreach (var quiz in quizes)
                    {
                        string qFile = ""; string fileName = "";
                        string qType = quiz.Attribute("type").Value;

                        if (qType == "multichoice" || qType == "essay" || qType == "truefalse")
                        {
                            string qName = quiz.Element("name").Element("text").Value;
                            string qText = quiz.Element("questiontext").Element("text").Value;

                            string removeImg = Regex.Replace(qText, @"<img\s[^>]*>(?:\s*?</img>)?", "", RegexOptions.IgnoreCase);


                            string _qText = removeImg;
                            #region Convert Image File to Base64 Encoded string

                            fileName = quiz.Element("questiontext").Element("file") is null ? "" : quiz.Element("questiontext").Element("file").Attribute("name").Value;
                            qFile = quiz.Element("questiontext").Element("file") is null ? "" : quiz.Element("questiontext").Element("file").Value;

                            if (!string.IsNullOrWhiteSpace(qFile))
                            {
                                string uploadedPath = Path.Combine(this._webHostEnvironment.ContentRootPath, "Upload/");
                                await System.IO.File.WriteAllBytesAsync(uploadedPath + fileName, Convert.FromBase64String(qFile));

                                string imageFile = uploadedPath + fileName;

                                #region callMediaAPI



                                var Mediaclient = new RestClient($"{_configUrl.MediaBaseUrl}/media/items/upload");
                                Mediaclient.Timeout = -1;
                                var Mediarequest = new RestRequest(Method.POST);
                                Mediarequest.AddParameter("isGenerateMediaId", true, ParameterType.QueryString);
                                Mediarequest.AddHeader("Authorization", token);
                                Mediarequest.AddFile("files", imageFile);
                                IRestResponse Mediaresponse = Mediaclient.Execute(Mediarequest);
                                mediaResponse = JsonConvert.DeserializeObject<MediaResponse>(Mediaresponse.Content).model;

                                #endregion

                                string newFilename = mediaResponse.uploadResponse.FirstOrDefault().newFilename;
                                _qText += "<figure class=\"image\"><img src = \"{mediaPreFix}" + newFilename + "{mediaPostFix}\"/></figure> ";

                            }
                            DTOAddQuestion MCQbody = new DTOAddQuestion();
                            DTOAddEssayQuestion Essaybody = new DTOAddEssayQuestion();
                            #endregion
                            string apiMethod = "mcq";
                            Guid CourseSubscriptionId = Guid.Parse(courseRequest.CourseSubscriptionId);

                            if (qType == "multichoice" || qType == "truefalse")
                            {
                                List<DTOAnswer> dTOAnswers = new List<DTOAnswer>();
                                IEnumerable<XElement> answers = quiz.Elements("answer");
                                foreach (var answer in answers)
                                {
                                    bool IsTrue = answer.Attribute("fraction").Value == "100";
                                    dTOAnswers.Add(new DTOAnswer
                                    {
                                        PlainText = answer.Element("text").Value,
                                        Text = answer.Element("text").Value,
                                        IsCorrect = IsTrue
                                    });
                                }

                                MCQbody = new DTOAddQuestion
                                {
                                    CourseSubscriptionId = CourseSubscriptionId,
                                    Version = new DTOVersion
                                    {
                                        Stem = new DTOStem
                                        {
                                            Text = _qText,
                                            PlainText = qName,
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
                                        ItemMappings = new List<DTOItemMapping>()
                                            {
                                                new DTOItemMapping
                                                {
                                                   // IloId = Guid.Parse(resultILO.Id.ToString()),
                                                    //LevelId =  resultleaf.Id
                                                   LevelId =  ParentId
                                                }
                                            }
                                    },
                                    MediaId = mediaResponse.mediaId,
                                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will change it

                                };
                            }
                            if (qType == "essay")
                            {
                                apiMethod = "essay";
                                Essaybody = new DTOAddEssayQuestion
                                {
                                    CourseSubscriptionId = CourseSubscriptionId,
                                    Version = new DTOEssayVersion
                                    {

                                        Stem = new DTOEssayStem
                                        {
                                            Direction = "FromMoodle",
                                            Text = _qText,
                                            PlainText = _qText,
                                            Answer = new DTOEssayAnswer
                                            {
                                                modelAnswer = "From Moodle",
                                                modelAnswerPlainText = "From Moodle"

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

                                        ItemMappings = new List<DTOItemMapping>
                                                {
                                                    new DTOItemMapping
                                                    {
                                                       // IloId = Guid.Parse(resultILO.Id.ToString()),
                                                        LevelId = ParentId
                                                    }
                                                }
                                    },
                                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will change it

                                };
                            }

                            if (qType == "truefalse")
                            {
                                apiMethod = "TF";
                            }
                            var mcqclient = new RestClient($"{_configUrl.QorrectBaseUrl}/item/" + apiMethod);
                            mcqclient.Timeout = -1;
                            var mcqrequest = new RestRequest(Method.POST);
                            mcqrequest.AddHeader("Authorization", token);
                            mcqrequest.AddHeader("Content-Type", "application/json");

                            if (qType == "multichoice" || qType == "truefalse")
                            {
                                mcqrequest.AddParameter("application/json", JsonConvert.SerializeObject(MCQbody), ParameterType.RequestBody);
                                IRestResponse mcqresponse = await mcqclient.ExecuteAsync(mcqrequest);

                                #region Log in Database

                                {
                                    int cID = 0;// Convert.ToInt32(courseRequest.CourseID);
                                    var logger = new DTORequestResponseLog
                                    {
                                        CourseID = Convert.ToInt32(Course.id),
                                        CourseName = Course.fullname,
                                        Device = "Moodel",
                                        ErrorQuestionID = cID,
                                        logRequest = JsonConvert.SerializeObject(MCQbody),
                                        logResponse = JsonConvert.SerializeObject(mcqresponse.Content),
                                        RequestUri = mcqclient.BaseUrl.AbsoluteUri,
                                        StatusCode = mcqresponse.StatusDescription,
                                        QuestionID = cID
                                    };
                                    await new CourseDataAccessLayer().RequestResponseLogger(bedoIntegrationString, logger);
                                }
                                #endregion
                            }
                            if (qType == "essay")
                            {
                                mcqrequest.AddParameter("application/json", JsonConvert.SerializeObject(Essaybody), ParameterType.RequestBody);

                                IRestResponse mcqresponse = await mcqclient.ExecuteAsync(mcqrequest);

                                #region Log in Database

                                {
                                    int cID = 0;// Convert.ToInt32(courseRequest.CourseID);
                                    var logger = new DTORequestResponseLog
                                    {
                                        CourseID = Convert.ToInt32(Course.id),
                                        CourseName = Course.fullname,
                                        Device = "Moodel",
                                        ErrorQuestionID = cID,
                                        logRequest = JsonConvert.SerializeObject(Essaybody),
                                        logResponse = JsonConvert.SerializeObject(mcqresponse.Content),
                                        RequestUri = mcqclient.BaseUrl.AbsoluteUri,
                                        StatusCode = mcqresponse.StatusDescription,
                                        QuestionID = cID
                                    };
                                    await new CourseDataAccessLayer().RequestResponseLogger(bedoIntegrationString, logger);
                                }
                                #endregion
                            }



                        }
                    }


                }

            }

            #endregion
            return Ok();
        }

        public string StripHTML(string inputHTML)
        {
            return Regex.Replace(inputHTML, @"<[^>]+>|&nbsp;|", "").Trim();//&ndash;
        }


        [HttpPost]
        [Route("LoadQuestionsFromXML")]
        public async Task<IActionResult> LoadQuestionsFromXML([FromBody] DTOAddCourseRequest courseRequest)
        {
            string token = $"Bearer {courseRequest.BearerToken}";

            string xmlfile = Path.Combine(this._webHostEnvironment.ContentRootPath, "DataSource/") + "Quiz.xml";
            XDocument xmlDoc = XDocument.Load(xmlfile);
            IEnumerable<XElement> quizes = xmlDoc.Descendants("question");
            foreach (var quiz in quizes)
            {
                string qType = quiz.Attribute("type").Value;
                string qName = quiz.Element("name").Element("text").Value;
                string qText = quiz.Element("questiontext").Element("text").Value;
                string qFile = quiz.Element("questiontext").Element("file").Value;
                string fileName = quiz.Element("questiontext").Element("file").Attribute("name").Value;

                #region Convert Image File to Base64 Encoded string

                string uploadedPath = Path.Combine(this._webHostEnvironment.ContentRootPath, "Upload/");
                await System.IO.File.WriteAllBytesAsync(uploadedPath + fileName, Convert.FromBase64String(qFile));

                #endregion

                List<DTOAnswer> dTOAnswers = new List<DTOAnswer>();
                IEnumerable<XElement> answers = quiz.Elements("answer");
                foreach (var answer in answers)
                {
                    //answer fraction="100" 
                    bool IsTrue = answer.Attribute("fraction").Value == "100";
                    dTOAnswers.Add(new DTOAnswer
                    {

                        Text = answer.Element("text").Value,
                        IsCorrect = IsTrue
                    });
                }


                Guid CourseSubscriptionId = Guid.Parse(courseRequest.CourseSubscriptionId);
                var mcqclient = new RestClient($"{_configUrl.QorrectBaseUrl}/item/mcq");
                mcqclient.Timeout = -1;
                var mcqrequest = new RestRequest(Method.POST);
                mcqrequest.AddHeader("Authorization", token);
                mcqrequest.AddHeader("Content-Type", "application/json");

                var MCQbody = new DTOAddQuestion
                {
                    CourseSubscriptionId = CourseSubscriptionId,
                    Version = new DTOVersion
                    {
                        Stem = new DTOStem
                        {
                            Text = qText,
                            PlainText = qName,
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


                        //Tags = _Tags,
                        ItemMappings = new List<DTOItemMapping>()
                    },
                    TransactionItemId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") // will change it

                };
                mcqrequest.AddParameter("application/json", JsonConvert.SerializeObject(MCQbody), ParameterType.RequestBody);
                IRestResponse mcqresponse = await mcqclient.ExecuteAsync(mcqrequest);

            }

            return Ok(quizes);
        }
    }
}
