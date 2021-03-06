﻿using System;
using System.IO;
using System.Text;

namespace PathUtils
{
    public sealed class LexicalPath
    {
        /// <summary>
        /// ToSlash returns the result of replacing each separator character
        /// in path with a slash ('/') character. Multiple separators are
        /// replaced by multiple slashes.
        /// </summary>
        /// <param name="path">The input path</param>
        /// <returns></returns>
        private static string ToSlash(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
                return path;
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// This is Go's path.Clean
        ///
        /// Clean returns the shortest path name equivalent to path
        /// by purely lexical processing. It applies the following rules
        /// iteratively until no further processing can be done:
        ///
        ///	1. Replace multiple slashes with a single slash.
        ///	2. Eliminate each . path name element (the current directory).
        ///	3. Eliminate each inner .. path name element (the parent directory)
        ///	   along with the non-.. element that precedes it.
        ///	4. Eliminate .. elements that begin a rooted path:
        ///	   that is, replace "/.." by "/" at the beginning of a path.
        ///
        /// The returned path ends in a slash only if it is the root "/".
        ///
        /// If the result of this process is an empty string, Clean
        /// returns the string ".".
        ///
        /// See also Rob Pike, ``Lexical File Names in Plan 9 or
        /// Getting Dot-Dot Right,''
        /// </summary>
        /// <param name="path">The input path</param>
        /// <returns></returns>

        // https://9p.io/sys/doc/lexnames.html
        public static string Clean(string path)
        {
            if (path == null || path.Length == 0)
                return ".";

            // Invariants:
            //	reading from path; r is index of next byte to process.
            //	writing to buf; w is index of next byte to write.
            //	dotdot is index in buf where .. must stop, either because
            //		it is the leading slash or it is a leading ../../.. prefix.

            StringBuilder sb = new StringBuilder();
            var rooted = path[0] == '/';
            int dotDot;
            int i;
            if (rooted)
            {
                sb.Append('/');
                i = 1;
                dotDot = 1;
            }
            else
            {
                i = 0;
                dotDot = 0;
            }

            while (i < path.Length)
            {
                if (path[i] == '/')
                {
                    // Empty path component
                    ++i;
                }
                else if (path[i] == '.' && ((i + 1) == path.Length || path[i + 1] == '/'))
                {
                    // '.' component. Skip.
                    ++i;

                }
                else if (path[i] == '.' && path[i + 1] == '.' && ((i + 2) == path.Length || path[i + 2] == '/'))
                {
                    // '..' component. Remove to the last seaparator.
                    i += 2;
                    if (sb.Length > dotDot)
                    {
                        var j = sb.Length - 1;
                        while (j > dotDot && sb[j] != '/')
                            --j;
                        sb.Remove(j, sb.Length - j);
                    }
                    else if (!rooted)
                    {
                        // cannot backtrack, but not rooted, so append .. element.
                        if (sb.Length > 0)
                            sb.Append('/');

                        sb.Append("..");
                        dotDot = sb.Length;
                    }
                }
                else
                {
                    // Real path element, add slash if needed.
                    if ((rooted && sb.Length != 1) || (!rooted && sb.Length != 0))
                        sb.Append('/');
                    for (; i < path.Length && path[i] != '/'; ++i)
                        sb.Append(path[i]);
                }
            }

            if (sb.Length == 0)
                return ".";

            return sb.ToString();
        }

        // This is Go's path.Join
        //
        // Join joins any number of path elements into a single path, adding a
        // separating slash if necessary. The result is Cleaned; in particular,
        // all empty strings are ignored.
        public static string Combine(params string[] args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in args)
            {
                if (s.Length > 0)
                {
                    sb.Append('/');
                    sb.Append(s);
                }
            }

            // Check for all strings empty
            if (sb.Length == 0)
                return "";

            // Remove the first slash - it was added for simplicity
            return Clean(sb.ToString(1, sb.Length - 1));
        }

        // This is Go's path.Ext
        //
        // Ext returns the file name extension used by path.
        // The extension is the suffix beginning at the final dot
        // in the final slash-separated element of path;
        // it is empty if there is no dot.
        public static string GetExtension(string path)
        {
            for (var i = path.Length - 1; i >= 0 && path[i] != '/'; --i)
                if (path[i] == '.')
                    return path.Substring(i);
            return "";
        }

        // This is Go's path.Split. C# doesn't have equivalent for it.
        //
        // Split splits path immediately following the final slash,
        // separating it into a directory and file name component.
        // If there is no slash in path, Split returns an empty dir and
        // file set to path.
        // The returned values have the property that path = dir+file.
        public static Tuple<string, string> Split(string path)
        {
            var id = path.LastIndexOf('/');
            if (id == -1)
                return Tuple.Create("", path);
            return Tuple.Create(path.Substring(0, id + 1), path.Substring(id + 1));
        }

        // This is Go's path.Dir
        //
        // Dir returns all but the last element of path, typically the path's directory.
        // After dropping the final element using Split, the path is Cleaned and trailing
        // slashes are removed.
        // If the path is empty, Dir returns ".".
        // If the path consists entirely of slashes followed by non-slash bytes, Dir
        // returns a single slash. In any other case, the returned path does not end in a
        // slash.
        public static string GetDirectoryName(string path)
        {
            var tuple = Split(path);
            return Clean(tuple.Item1);
        }

        // This is Go's path.Base
        // Base returns the last element of path.
        // Trailing slashes are removed before extracting the last element.
        // If the path is empty, Base returns ".".
        // If the path consists entirely of slashes, Base returns "/".
        public static string GetFileName(string path)
        {
            if (path.Length == 0) return ".";

            // Strip trailing slashes
            StringBuilder sb = new StringBuilder(path);
            var id = sb.Length - 1;
            while (id > 0 && sb[id] == '/')
                --id;

            if (id < sb.Length - 1)
                sb.Remove(id + 1, sb.Length - id - 1);

            // Find the last element
            id = sb.Length - 1;
            while (id >= 0 && path[id] != '/')
                --id;

            if (id >= 0)
                sb.Remove(0, id + 1);

            // If empty now, it had only slashes.
            if (sb.Length == 0)
                return "/";

            return sb.ToString();
        }
    }
}
