using System.Collections.Generic;

namespace Qorrect.Integration.Models
{

    public class Cours
    {
        public string fullname { get; set; }
        public string shortname { get; set; }
        public string id { get; set; }
        public string idnumber { get; set; }
        public string summary { get; set; }
        public string category { get; set; }
    }

    public class DTOTag
    {
        public string id { get; set; }
        public string name { get; set; }
    }


    public class DTOModleCourse
    {
        public List<Cours> courses { get; set; }
    }

    public class ModelUnit
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool visible { get; set; }
        public List<modellesson> modules { get; set; }//lessons
    }

    public class modellesson
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool visible { get; set; }
    }

    public class UploadResponse
    {
        public int fileUploadStatus { get; set; }
        public string filename { get; set; }
        public object messages { get; set; }
        public string newFilename { get; set; }
    }

    public class Model
    {
        public string mediaId { get; set; }
        public List<UploadResponse> uploadResponse { get; set; }
    }

    public class MediaResponse
    {
        public Model model { get; set; }
        public List<object> messages { get; set; }
        public bool isValid { get; set; }
        public int subTotalCount { get; set; }
        public object status { get; set; }
    }


}
