using Qorrect.Integration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Qorrect.Integration.Services
{
    public class CourseDataAccessLayer
    {
        public async Task<List<DTOBedoCourse>> GetAllCourses(string connectionString)
        {
            List<DTOBedoCourse> lstCourse = new List<DTOBedoCourse>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("spGetAllCourses", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    lstCourse.Add(new DTOBedoCourse()
                    {
                        Id = Convert.ToInt32(rdr["Id"]),
                        CourseName = rdr["CourseName"].ToString(),
                        CourseCode = rdr["CourseCode"].ToString(),
                        Description = rdr["Description"].ToString(),
                        ClassesHours = Convert.ToInt32(rdr["ClassesHours"]),
                        CreditHours = Convert.ToInt32(rdr["CreditHours"]),
                        LectureHours = Convert.ToInt32(rdr["LectureHours"]),
                        PracticalHours = Convert.ToInt32(rdr["PracticalHours"]),
                        TotalMarks = Convert.ToDouble(rdr["TotalMarks"])
                    });
                }
                con.Close();
            }
            return lstCourse.ToList();
        }

        public async Task<List<DTOBedoUnites>> GetCourseLevels(string connectionString , int crsId)
        {
            List<DTOBedoUnites> crsLevels = new List<DTOBedoUnites>();
            int order = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spGetLevelByCourseId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@crsId", crsId);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {

                        using (DataSet ds = new DataSet())
                        {

                            da.Fill(ds);
                            con.Close();

                            if (ds.Tables[1].Rows.Count > 1)
                            {
                                order++;
                                crsLevels = ds.Tables[0].AsEnumerable().Select(QdataRow => new DTOBedoUnites
                                {
                                    Id = QdataRow.Field<int>("ID"),
                                    Name = QdataRow.Field<string>("Name"),
                                    Code = QdataRow.Field<string>("Code"),
                                    Order = order,
                                    Lessons = ds.Tables[1].AsEnumerable().Select(AdataRow => new DTOBedoLessons
                                    {
                                        Id = AdataRow.Field<int>("ID"),
                                        Name = AdataRow.Field<string>("Name"),
                                        Code = AdataRow.Field<string>("Code"),
                                        Order = order,
                                        ParentId = AdataRow.Field<int>("ID_Ref")
                                    }).Where(a => a.ParentId == QdataRow.Field<int>("ID")).ToList()
                                }).ToList();

                            }
                        }

                    }
                }
            }
            return crsLevels.ToList();
        }

        public async Task<List<DTOBedoILO>> GetLevelIlo(string connectionString , int levelId)
        {
            List<DTOBedoILO> Ilos = new List<DTOBedoILO>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spGetILOByLevelId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@levelId", levelId);

                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        Ilos.Add(new DTOBedoILO()
                        {
                            Id = Convert.ToInt32(rdr["ID"].ToString()),
                            Name = rdr["Name"].ToString(),
                            Code = rdr["Code"].ToString(),
                            CognitiveName = rdr["CognitiveName"].ToString()
                        });
                    }
                    con.Close();
                }
            }
            return Ilos.ToList();
        }

        public async Task<List<DTOBedoCongnitiveLevel>> GetCongitive(string connectionString , int crsId)
        {
            List<DTOBedoCongnitiveLevel> congnitiveLevels = new List<DTOBedoCongnitiveLevel>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetCongitiveByCrsId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@crsid", crsId);

                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        congnitiveLevels.Add(new DTOBedoCongnitiveLevel()
                        {
                            Id = Convert.ToInt32(rdr["Id"].ToString()),
                            Name = rdr["Name"].ToString(),
                            Code = rdr["Code"].ToString()
                        });
                    }
                    con.Close();
                }
            }
            return congnitiveLevels.ToList();
        }

        public async Task<List<DTOItemFromBedoByIloResponse>> GetItemsByIlo(string connectionString , int IloId)
        {
            List<DTOItemFromBedoByIloResponse> items = new List<DTOItemFromBedoByIloResponse>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spGetItemsByIlo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IloId", IloId);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        using (DataSet ds = new DataSet())
                        {
                            da.Fill(ds);
                            con.Close();

                            if (ds.Tables[1].Rows.Count > 1)
                            {
                                items = ds.Tables[0].AsEnumerable().Select(QdataRow => new DTOItemFromBedoByIloResponse
                                {
                                    Id = QdataRow.Field<int>("QuestionID"),
                                    Stem = QdataRow.Field<string>("Question"),
                                    QuestionTypeID = QdataRow.Field<int>("QuestionTypeID"),
                                    Answers = ds.Tables[1].AsEnumerable().Select(AdataRow => new DTOItemAnswersFromBedoByIloResponse
                                    {
                                        QuestionId = AdataRow.Field<int>("QuestionID"),
                                        Answer = AdataRow.Field<string>("Answer"),
                                        TrueFalse = AdataRow.Field<bool>("TrueFalse")
                                    }).Where(a => a.QuestionId == QdataRow.Field<int>("QuestionID")).ToList()
                                }).ToList();

                            }
                        }

                    }
                }
            }
            return items.ToList();

        }

        public async Task<int> InvisibleAddedCourses(string connectionString , string ids)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spInvisibleAddedCourses", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ids", ids);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
            return 1;
        }

        public async Task RequestResponseLogger(string bedoIntegrationString, DTORequestResponseLog model)
        {
            using (SqlConnection con = new SqlConnection(bedoIntegrationString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_LOGREQUESTANDRESPONSE", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Req", model.logRequest);
                    cmd.Parameters.AddWithValue("@Res", model.logResponse);
                    cmd.Parameters.AddWithValue("@Uri", model.RequestUri);
                    cmd.Parameters.AddWithValue("@CrsId", model.CourseID);
                    cmd.Parameters.AddWithValue("@QuesId", model.QuestionID);
                    cmd.Parameters.AddWithValue("@StCode", model.StatusCode);
                    cmd.Parameters.AddWithValue("@Dev", model.Device);
                    cmd.Parameters.AddWithValue("@CourseName", model.CourseName);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public async Task MoodleConfigurationSetting(string bedoIntegrationString, DTOManageUrl model)
        {
            using (SqlConnection con = new SqlConnection(bedoIntegrationString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_MOODLECONFIG", con))
                {
                    con.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@MBURL", model.MoodlebaseUrl);
                    cmd.Parameters.AddWithValue("@QBURL", model.QorrectBaseUrl);
                    cmd.Parameters.AddWithValue("@DBURL", model.MediaBaseUrl);
                    var rdr = cmd.ExecuteNonQuery();                 
                    con.Close();
                }
            }
        }

        public async Task BedoConfigurationSetting(string bedoIntegrationString, DTOManageUrl model)
        {
            using (SqlConnection con = new SqlConnection(bedoIntegrationString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_QORRECTCONFIG", con))
                {
                    con.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BBURL", model.BedobaseUrl);
                    cmd.Parameters.AddWithValue("@QBURL", model.QorrectBaseUrl);
                    cmd.Parameters.AddWithValue("@DBURL", model.MediaBaseUrl);
                    var rdr = cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public async Task<DTOManageUrl> GetMoodleBaseUrl(string bedoIntegrationString)
        {
            DTOManageUrl _managedUrl = null;
            List<DTOConfigUrl> _configUrls = new List<DTOConfigUrl>();
            using (SqlConnection con = new SqlConnection(bedoIntegrationString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GETCONFIGBASEURL", con))
                {
                    con.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        _configUrls.Add(new DTOConfigUrl
                        {
                            Faculty = rdr[0].ToString(),
                            ApiUrl = rdr[1].ToString()
                        });
                    }

                    _managedUrl = new DTOManageUrl
                    {
                        BedobaseUrl = _configUrls.FirstOrDefault(a => a.Faculty.Equals("BedoConnectionString")).ApiUrl,
                        MediaBaseUrl = _configUrls.FirstOrDefault(a => a.Faculty.Equals("MediaBaseUrl")).ApiUrl,
                        MoodlebaseUrl = _configUrls.FirstOrDefault(a => a.Faculty.Equals("MoodleBaseUrl")).ApiUrl,
                        QorrectBaseUrl = _configUrls.FirstOrDefault(a => a.Faculty.Equals("QorrectBaseUrl")).ApiUrl
                    };

                    con.Close();
                }
            }
            return _managedUrl;
        }

        public async Task<List<DTOTransferedCourse>> GetTransferedCourses(string bedoIntegrationString)
        {
            List<DTOTransferedCourse> lstCourse = new List<DTOTransferedCourse>();
            using (SqlConnection con = new SqlConnection(bedoIntegrationString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetTransferedCourses", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    lstCourse.Add(new DTOTransferedCourse()
                    {
                        Id = Convert.ToInt32(rdr["CourseID"]),
                        CourseName = rdr["CourseName"].ToString(),
                        // CourseCode = rdr["total"].ToString(),
                        InsertedItems = ((Convert.ToInt32(rdr["ok"].ToString()))-1).ToString(),
                        LostItems = rdr["lost"].ToString(),
                    });
                }
                con.Close();
            }
            return lstCourse.ToList();
        }
    }
}
