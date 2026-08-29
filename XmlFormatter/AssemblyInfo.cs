using System.Runtime.CompilerServices;

// StringWriterWithEncoding is internal: it exists to give XmlWriter an encoding to read off
// the writer, which is an implementation detail of Minimize rather than API. Its tests still
// construct it directly, including the null-encoding fallback that Minimize cannot reach.
[assembly: InternalsVisibleTo("XmlFormatter.Tests")]
