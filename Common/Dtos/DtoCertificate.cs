using System;

namespace Common.Dtos
{
    public class DtoCertificate
    {
        public int Id { get; set; }
        public string CertificateCode { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public DateTime IssuedAt { get; set; }
        public string? StudentName { get; set; }
        public string? CourseTitle { get; set; }
    }
}
