
/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;

namespace Autodesk.Aps.Models
{
    public class ZipArchiveEntry
    {
        /// <summary>
        /// Gets or sets the optional entry comment.
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Gets the compressed size of the entry in the zip archive.
        /// </summary>
        public long CompressedLength { get; set; }
        /// <summary>
        /// The 32-bit Cyclic Redundant Check.
        /// </summary>
        [System.CLSCompliant(false)]
        public uint Crc32 { get; set; }
        /// <summary>
        /// OS and application specific file attributes.
        /// </summary>
        public int ExternalAttributes { get; set; }
        /// <summary>
        /// Gets the relative path of the entry in the zip archive.
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// Gets or sets the last time the entry in the zip archive was changed.
        /// </summary>
        public DateTimeOffset LastWriteTime { get; set; }
        /// <summary>
        /// Gets the uncompressed size of the entry in the zip archive.
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// Gets the file name of the entry in the zip archive.
        /// </summary>
        public string Name { get; set; }
    }
}