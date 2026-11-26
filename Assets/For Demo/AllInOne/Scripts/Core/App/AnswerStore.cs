using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Keeps the answers of the tasks in one file on this device. Nothing is sent anywhere, and the user can delete the file
    /// in the app.
    /// </summary>
    public static class AnswerStore
    {
        private const string Header = "Time,Session,Answer,Value";

        /// <summary>Folder of the file. Empty = the data folder of the app. Tests use their own.</summary>
        public static string Folder { get; set; }

        /// <summary>The file with the answers.</summary>
        public static string FilePath => Path.Combine(string.IsNullOrEmpty(Folder) ? Application.persistentDataPath : Folder, "all-in-one-answers.csv");

        public static bool HasAnswers => File.Exists(FilePath);

        /// <summary>Counts the changes, so that panels need not look at the disk.</summary>
        public static int Version { get; private set; }

        /// <summary>Appends one answer. Problems with the disk are logged and never stop the session.</summary>
        public static void Add(string session, string answer, string value)
        {
            try
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string line = string.Join(",", time, session, answer, value) + "\n";
                File.AppendAllText(FilePath, HasAnswers ? line : Header + "\n" + line);
                Version++;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("All-in-One: an answer could not be saved: " + e.Message);
            }
        }

        /// <summary>Deletes all answers. Returns false when the file could not be removed.</summary>
        public static bool DeleteAll()
        {
            try
            {
                if (HasAnswers) File.Delete(FilePath);
                Version++;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("All-in-One: the answers could not be deleted: " + e.Message);
                return false;
            }
        }
    }
}
