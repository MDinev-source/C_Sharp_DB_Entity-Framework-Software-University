﻿namespace SoftJail.DataProcessor.ImportDto
{
    using System.ComponentModel.DataAnnotations;
    public class ImportMailDto
    {
        [Required]
        public string Description { get; set; }
        [Required]
        public string Sender { get; set; }
        [Required]
        [RegularExpression(@"^[\w ]+ str\.$")]
        public string Address { get; set; }
    }
}
