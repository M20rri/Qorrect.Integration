using Microsoft.AspNetCore.Http;
using Qorrect.Integration.Helper;
using System;
using System.Collections.Generic;

namespace Qorrect.Integration.Models
{
    public class DTOAddCourseRequest
    {
        public string BearerToken { get; set; }
        public string CourseSubscriptionId { get; set; }
        public List<DTOBedoCourse> Courses { get; set; }
    }

    public class DTOAddModleCourseRequest
    {
        public string BearerToken { get; set; }
        public string ModleToken { get; set; }
        public string CourseSubscriptionId { get; set; }
        public IFormFile XMLFile { get; set; }
        public string Course { get; set; }

        public string CourseID { get; set; }
    }

    public class DTOAnswer
    {
        public string Text { get; set; }
        public string PlainText { get; set; }
        public string Comment { get; set; }
        public object Hint { get; set; }
        public object Feedback { get; set; }
        public int Weight { get; set; }
        public int Order { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class DTOEssayAnswer
    {
        public string modelAnswer { get; set; }
        public string modelAnswerPlainText { get; set; }
        public string Comment { get; set; }
        public object Hint { get; set; }
        public object Feedback { get; set; }
        public int Weight { get; set; }
        public int Order { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class DTOSettings
    {
        public bool IsShuffleAnswers { get; set; }
        public bool IsAllowForTrialExams { get; set; }
        public int Difficulty { get; set; }
        public int ExpectedTime { get; set; }
        public bool IsAllowedForComputerBasedOnly { get; set; }
    }

    public class DTOStem
    {
        public string Text { get; set; }
        public string PlainText { get; set; }
        public List<DTOAnswer> Answers { get; set; }
        public DTOSettings Settings { get; set; }
        public string Comment { get; set; }
        public object Hint { get; set; }
        public object Feedback { get; set; }
        public int Difficulty { get; set; }
    }
    public class DTOEssayStem
    {
        public string Text { get; set; }
        public string PlainText { get; set; }
        public string Direction { get; set; }
        public DTOEssayAnswer Answer { get; set; }
        public DTOSettings Settings { get; set; }
        public string Comment { get; set; }
        public object Hint { get; set; }
        public object Feedback { get; set; }
        public int Difficulty { get; set; }
    }

    public class DTOItemMapping
    {
        public Guid? LevelId { get; set; }
        public Guid? IloId { get; set; }
    }

    public class DTOVersion
    {
        public DTOStem Stem { get; set; }
        public int ItemClassification { get; set; }
        public List<DTOTag> Tags { get; set; }
        public List<DTOItemMapping> ItemMappings { get; set; }
    }
    public class DTOEssayVersion
    {
        public DTOEssayStem Stem { get; set; }
        public int ItemClassification { get; set; }

        public List<DTOTag> Tags { get; set; }
        public List<DTOItemMapping> ItemMappings { get; set; }
    }

    public class DTOAddQuestion
    {
        public Guid? ItemId { get; set; }
        public Guid CourseSubscriptionId { get; set; }
        public string MediaId { get; set; }
        public string ClonedMediaId { get; set; }
        public DTOVersion Version { get; set; }
        public Guid? TransactionItemId { get; set; }
    }
    public class DTOAddEssayQuestion
    {
        public Guid? ItemId { get; set; }
        public Guid CourseSubscriptionId { get; set; }
        public string MediaId { get; set; }
        public string ClonedMediaId { get; set; }
        public DTOEssayVersion Version { get; set; }
        public Guid? TransactionItemId { get; set; }
    }

    public class DTOAddEditCourse
    {
        public Guid? Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? CourseSubscriptionId { get; set; }
        public DTOCourseData CourseData { get; set; }
    }

    public class DTOCourseData
    {
        public CourseType? CourseType { get; set; }
        public int? CreditHours { get; set; }
        public int? LecturesHours { get; set; }
        public int? PracticalHours { get; set; }
        public string Description { get; set; }
        public double? TotalMarks { get; set; }
        public double? TotalHours { get; set; }
        public List<DTOTag> Tags { get; set; }
        public bool IsValidTotalMarks { get { return (TotalMarks == null || TotalMarks >= 0); } }
        public bool IsValidTotalHours { get { return (TotalHours == null || TotalHours >= 0); } }
    }

    public class DTOLevelCourseDataTag
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
    }
    public class DTOTransferedCourse
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string InsertedItems { get; set; }
        public string LostItems { get; set; }
        public string ResponseBody { get; set; }
        
        public string QuestionID { get; set; }
        

    }
    public class DTOBedoCourse
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int CreditHours { get; set; }
        public int PracticalHours { get; set; }
        public int ClassesHours { get; set; }
        public int LectureHours { get; set; }
        public string Description { get; set; }
        public double? TotalMarks { get; set; }
    }

    public class DTOAddEditNodeLevel
    {
        public DTOAddEditNodeLevel()
        {
            TeachingHours = Marks = ILOCount = ChildrenCount = LeafsCount = 0;
        }
        public Guid? Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string ChildOutlineNodeName { get; set; }
        public NodeType? ChildOutlineNodeType { get; set; }
        public int? ChildrenCount { get; set; }
        public int? LeafsCount { get; set; }
        public double? Marks { get; set; }
        public int? ILOCount { get; set; }
        public double? TeachingHours { get; set; }
        public int? Order { get; set; }
        public string LeafOutlineNodeName { get; set; }
    }

    public class CourseLeaf
    {
        public CourseLeaf()
        {
            IntendedLearningOutcomes = new List<Guid>();
        }

        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public double? TeachingHours { get; set; }
        public double? Marks { get; set; }
        public int? Weight { get; set; }
        public Guid? ParentId { get; set; }
        public List<Guid> IntendedLearningOutcomes { get; set; }
        public int? Order { get; set; }
        public List<CourseLeaf> leaves { get; set; }
    }


    public class DTOBedoUnites
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int? Order { get; set; }
        public double? Marks { get; set; }

        public List<DTOBedoLessons> Lessons { get; set; }
    }

    public class DTOBedoLessons
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int? Order { get; set; }
        public int? ParentId { get; set; }

    }

    public class DTOApplyOutlineStructure
    {
        public Guid Id { get; set; }
    }

    public class DTOAddCourseCognitiveLevelRequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid CourseId { get; set; }
    }

    public class DTOCognitiveLevelResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid CourseId { get; set; }
    }


    public class DTOBedoILO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string CognitiveName { get; set; }
    }


    public class DTOBedoCongnitiveLevel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }


    public class DTOQorrectILORequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid CourseId { get; set; }
        public Guid? CourseStandardId { get; set; }
        public Guid CourseCognitiveLevelId { get; set; }
        public string CourseCognitiveLevelName { get; set; }
    }

    public class DTOItemFromBedoByIloResponse
    {
        public int Id { get; set; }
        public int QuestionTypeID { get; set; }
        public string Stem { get; set; }
        public List<DTOItemAnswersFromBedoByIloResponse> Answers { get; set; }
    }

    public class DTOItemAnswersFromBedoByIloResponse
    {
        public int QuestionId { get; set; }
        public int QuestionTypeID { get; set; }
        public string Answer { get; set; }
        public bool TrueFalse { get; set; }

    }

    public class DTOTags
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class DTOQorrectModulesResponse
    {
        public int totalCountWithoutFilter { get; set; }
        public List<List> list { get; set; }
    }

    public class List
    {
        public string id { get; set; }
        public string fullName { get; set; }
        public string code { get; set; }
        public int subjectCreatorCount { get; set; }
        public int activatedSubjectCreatorCount { get; set; }
        public int deactivatedSubjectCreatorCount { get; set; }
        public int teachersCount { get; set; }
        public int activatedTeachersCount { get; set; }
        public int deactivatedTeachersCount { get; set; }
        public int studentCount { get; set; }
        public int activatedStudentCount { get; set; }
        public int deactivatedStudentCount { get; set; }
        public bool isPublished { get; set; }
        public int managersCount { get; set; }
        public int activatedManagersCount { get; set; }
        public int deactivatedManagersCount { get; set; }
        public int writersCount { get; set; }
        public int activatedWritersCount { get; set; }
        public int deactivatedWritersCount { get; set; }
        public int reviewersCount { get; set; }
        public int activatedReviewersCount { get; set; }
        public int deactivatedReviewersCount { get; set; }
        public string description { get; set; }
        public bool isArchiveManagers { get; set; }
        public bool isArchiveReviewers { get; set; }
        public bool isArchiveWriters { get; set; }
        public bool isArchiveTeachers { get; set; }
        public bool isArchiveStudents { get; set; }
        public bool isArchiveSubjectCreators { get; set; }
    }
}