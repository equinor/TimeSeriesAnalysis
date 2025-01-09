using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that holds comments added to models.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Author of comment.
        /// </summary>
        public string author;
        /// <summary>
        /// Date of comment.
        /// </summary>
        public DateTime date;
        /// <summary>
        /// Comment string
        /// </summary>
        public string comment;
        /// <summary>
        /// Plant score, intended to hold manully set values indicating specific statuses of the model.
        /// </summary>
        public double plantScore;

        /// <summary>
        /// Comment constructor.
        /// </summary>
        /// <param name="author"></param>
        /// <param name="date"></param>
        /// <param name="comment"></param>
        /// <param name="plantScore"></param>
        public Comment(string author, DateTime date, string comment, double plantScore = 0)
        {
            this.author = author;
            this.date = date;
            this.comment = comment;
            this.plantScore = plantScore;
        }
    }
}
