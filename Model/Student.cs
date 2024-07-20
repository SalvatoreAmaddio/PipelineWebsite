using Backend.Model;
using System.Data.Common;

namespace Pipeline
{
    /// <summary>
    /// The Record class represents a record of a <see cref="HtmlTablePage"/>
    /// </summary>
    [Table(nameof(Student))]
    public class Student : AbstractSQLModel
    {
        #region Properties
        /// <summary>
        /// Gets the student's ID.
        /// </summary>
        [PK]
        public long StudentID { get; set; }

        /// <summary>
        /// Gets or sets the student's full name.
        /// </summary>
        [Field]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the student's contact information.
        /// </summary>
        [Field]
        public string Contact { get; } = string.Empty;

        /// <summary>
        /// Gets the student's program and university information.
        /// </summary>
        [Field]
        public string ProgramUniversity { get; } = string.Empty;
        #endregion

        #region Constructors
        public Student() =>
        InsertQry = $"INSERT INTO {nameof(Student)} (StudentID, FullName, Contact, ProgramUniversity) VALUES (@StudentID, @FullName, @Contact, @ProgramUniversity)";

        /// <summary>
        /// Initializes a new instance of the Student class using data from a string array.
        /// </summary>
        /// <param name="dataRow">An array of strings containing the student's data.</param>
        public Student(string[] dataRow) : this()
        {
            try
            {
                StudentID = Convert.ToInt32(dataRow[0]);
            }
            catch
            {
                return;
            }
            FullName = dataRow[1].Replace("\n", "").Trim();
            Contact = dataRow[2].Replace("\n", "").Trim().RemoveExtraSpaces();
            ProgramUniversity = dataRow[4].Replace("\n", "").Trim().RemoveExtraSpaces();
        }

        public Student(DbDataReader reader) : this()
        {
            StudentID = reader.GetInt64(0);    
            FullName = reader.GetString(1);
            Contact = reader.GetString(2);
            ProgramUniversity = reader.GetString(3);
        }
        #endregion

        public override ISQLModel Read(DbDataReader reader) => new Student(reader);

        /// <summary>
        /// Checks if the record is valid.
        /// </summary>
        /// <returns>true if the record's ID is greater than 0; otherwise, false.</returns>
        public bool IsValid() => StudentID > 0;
        public override bool Equals(object? obj) => obj is Student student && StudentID == student.StudentID;
        public override int GetHashCode() => HashCode.Combine(StudentID);
        public override string ToString() => FullName;

    }
}