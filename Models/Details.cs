using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resume_NCDL.Models
{
    public class Details
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public byte[] ResumeData { get; set; }
        public string ResumeFileName { get; set; }
        public string ResumeContentType { get; set; }
    }
}
