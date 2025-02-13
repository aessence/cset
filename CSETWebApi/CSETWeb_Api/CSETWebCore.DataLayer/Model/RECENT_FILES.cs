﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CSETWebCore.DataLayer.Model
{
    public partial class RECENT_FILES
    {
        [StringLength(512)]
        public string AssessmentName { get; set; }
        [StringLength(900)]
        public string Filename { get; set; }
        [StringLength(1024)]
        public string FilePath { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime LastOpenedTime { get; set; }
        [Key]
        public int RecentFileId { get; set; }
    }
}