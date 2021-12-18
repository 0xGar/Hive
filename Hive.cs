using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace HiveSpace
{
    public static class Hive
    {
        const int T_NONE = 0x0000;
        const int T_STRING = 0x0001;
        const int T_STRING2 = 0x0002;
        const int T_BINARY = 0x0003;
        const int T_DWORD_SMALLE = 0x0004;
        const int T_DWORD_BIGE = 0x00005;
        const int T_LINK = 0x00006;
        const int T_MULTISTRING = 0x0007;
        const int T_RESOURCELIST = 0x00008;
        const int T_RESOURCEDESCRIPTION = 0x00009;
        const int T_RESOURCEREQUIREMENT = 0x00000A;
        const int T_QWORD = 0x000B;
        const int T_FILETIME = 0x0010;

        /*
            string NTUSER = @"C:\New Folder\NTUSER - Copy.DAT";
            BinaryReader br = new BinaryReader(File.Open(NTUSER, FileMode.Open));
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            int length = (int)br.BaseStream.Length;
            byte[] file = br.ReadBytes(length);

            String[] s = new String[] { @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}", "AppEvents", "EventLabels", "ActivatingDocument" };
            List<Dictionary<String, String>> key = search(file, s);

            if (key.Count < 1)
                Console.Write("No key found");
            foreach (Dictionary<String, String> k in key)
            {
                Console.Write("Key Name: " + k["name"] + ", Type: " + k["type"] + ", Data: " + k["data"] + "\n");
            }*/

        public static List<Dictionary<String, String>> search(byte[] file, String[] search)
        {
            List<Dictionary<String, String>> key = new List<Dictionary<String, String>>();
            int length = file.Length;
            for (int index = 0; index < length; index++)
            {
                //first nk record is root, which is CsiTool....
                if ((char)file[index] == 'n' && (char)file[index + 1] == 'k')
                {
                    key = (List<Dictionary<String, String>>)hive(index - 4, file, 0, false, search, 0, new List<Dictionary<String, String>>());
                    break;
                }
            }
            return (key);
        }
        public static Object hive(long index, byte[] b, int indent, bool isKv, String[] search, int pointer, List<Dictionary<String, String>> rtn)
        {
            int size, subkeyIndex, signature, subkeyCount, nameLength, valueCount, valueIndex;
            //dataLength, type;
            String nameString = "";
            //byte[] name;
            //byte[] data;

            if (index + 5 < b.Length && ((char)b[index + 4]) == 'n' && ((char)b[index + 5]) == 'k')
            {
                //the nk format is systematic. every bytes x to z contain data we're interested in
                //typical format: [datasize]...[signature]...[number of subkeys]...
                size = EndianByteLilToBig32(new byte[] { b[index], b[index + 1], b[index + 2], b[index + 3] });
                signature = EndianByteBigToBig32(new byte[] { b[index + 4], b[index + 5] });
                subkeyCount = EndianByteLilToBig32(new byte[] { b[index + 0x18], b[index + 0x19], b[index + 0x1A], b[index + 0x1B] });
                subkeyIndex = EndianByteLilToBig32(new byte[] { b[index + 0x20], b[index + 0x21], b[index + 0x22], b[index + 0x23] });
                subkeyIndex += 0x1000;
                valueCount = EndianByteLilToBig32(new byte[] { b[index + 0x28], b[index + 0x29], b[index + 0x2A], b[index + 0x2B] });
                valueIndex = EndianByteLilToBig32(new byte[] { b[index + 0x2C], b[index + 0x2D], b[index + 0x2E], b[index + 0x2F] });
                valueIndex += 0x1000;
                nameLength = EndianByteLilToBig32(new byte[] { b[index + 0x4c], b[index + 0x4d] });
                //name = new byte[nameLength+1];

                //print name of the key directory
                for (int i = 0; i < nameLength; i++)
                    nameString += (char)b[index + 0x50 + i];

                //Console.Write("\nZzZzZzZ, " + pointer.ToString() + " count: " + subkeyCount.ToString());

                if (search.Length > 0 && ((pointer >= search.Length) || !nameString.ToLower().Equals(search[pointer].ToLower())))
                    return (rtn);

                /* for (int i = 0; i < indent; i++)
                     Console.Write("  ");
                 //print name of the key directory
                 Console.Write(""+nameString+"\n");*/

                //when searching, only get keys for final key
                if (valueCount > 0 && (search.Length < 1 || search.Length == (pointer + 1)))
                {
                    hive(valueIndex, b, indent + 2, true, search, pointer, rtn);
                    // Console.Write("HEREEEEEEEEEE:" + rtn.Count);
                    return (rtn);
                }
                //when subkeys exist and (there's no search string, or we haven't reached final key so must keep searching)
                if (subkeyCount > 0 && (search.Length < 1 || (pointer < search.Length)))
                    hive(subkeyIndex, b, indent + 1, false, search, pointer + 1, rtn);
            }
            //these values are like pointer lists, going to another list, a kv, or nk, or db? maybe
            else if (index + 5 < b.Length &&
                (
                    ((char)b[index + 4] == 'l' && (char)b[index + 5] == 'f') ||
                    ((char)b[index + 4] == 'l' && (char)b[index + 5] == 'h') ||
                    ((char)b[index + 4] == 'l' && (char)b[index + 5] == 'i') ||
                    ((char)b[index + 4] == 'r' && (char)b[index + 5] == 'i')
                ))
            {
                size = EndianByteLilToBig32(new byte[] { b[index], b[index + 1], b[index + 2], b[index + 3] });
                signature = EndianByteBigToBig32(new byte[] { b[index + 0x4], b[index + 0x5] });
                subkeyCount = EndianByteLilToBig32(new byte[] { b[index + 0x06], b[index + 0x07] });
                subkeyIndex = EndianByteLilToBig32(new byte[] { b[index + 0x08], b[index + 0x09], b[index + 0xA], b[index + 0xB] });


                //lf and lh have a 4 byte hash after every address pointing to a record location
                //however, ri and li do not. we want to skip the 4 byte hash stuff when applicable
                int jumpby = 8;

                if (
                    ((char)b[index + 4] == 'r' && (char)b[index + 5] == 'i') ||
                    ((char)b[index + 4] == 'l' && (char)b[index + 5] == 'i')
                    )
                    jumpby = 4;
                //for every subkey, get the pointer to it and go to it
                for (long i = 0, j = index + 0x08; i < subkeyCount; i++)
                {

                    subkeyIndex =
                        0x1000 +
                        EndianByteLilToBig32(new byte[] { b[j], b[j + 1], b[j + 2], b[j + 3] });
                    hive(subkeyIndex, b, indent + 1, false, search, pointer, rtn);
                    //Console.Write("\n>>"+subkeyCount+" ");
                    j = j + jumpby;
                }
            }
            else if ((index + 5 < b.Length) && (char)b[index + 4] == 'v' && (char)b[index + 5] == 'k')
            {
                //In theory, should never be entered. vk keys are gotten from a list without a signature,
                //which is processed below. the below portion of the code checks if the list points to
                //kv, and if so processes them in a loop and returns an object with that data.
                //
                //however
                //if the list contains a pointer to another list with a kv record, I don't think it'll
                //work and instead we'll end up in here. if so the below mode must be modified, or somehow
                //adjust the return values of this code so that no keys are lost
            }
            //data set, containing pointers to vk. **to do: check for db**
            else if (index + 5 < b.Length && isKv)
            {
                //size is negative if in use. make positive for real value
                size = EndianByteLilToBig32(new byte[] { b[index], b[index + 1], b[index + 2], b[index + 3] });
                size = (~size) + 1;

                //0x04-0x8 is the pointer to the kv (or other) key. this is 4 bytes. the size tells us the
                //total size (including the part telling us the size). divide it by eight so we can jump
                //from pointer to pointer
                for (long i = 0, j = index + 0x04; i < (size / 4); i++, j += 4)
                {
                    //else is a list
                    subkeyIndex =
                        0x1000 +
                        EndianByteLilToBig32(new byte[] { b[j], b[j + 1], b[j + 2], b[j + 3] });
                    if ((char)b[subkeyIndex + 4] == 'v' && (char)b[subkeyIndex + 5] == 'k')
                    {
                        Dictionary<String, String> temp = new Dictionary<String, String>();
                        Dictionary<String, Object> kv = get_kv(subkeyIndex, b);

                        temp["name"] = (String)kv["name"];
                        if (((String)kv["type"]).Equals("String"))
                        {
                            temp["type"] = "String";
                            temp["data"] = (String)kv["data"];
                        }

                        else if (((String)kv["type"]).Equals("DWord"))
                        {
                            temp["type"] = "DWord";
                            temp["data"] = "0x" + ((uint)kv["data"]).ToString("X");
                        }
                        else if (((String)kv["type"]).Equals("QWord"))
                        {
                            temp["type"] = "QWord";
                            temp["data"] = "0x" + ((UInt64)kv["data"]).ToString("X");
                        }

                        else if (((String)kv["type"]).Equals("Binary"))
                        {
                            temp["type"] = "Binary";
                            String temp2 = "";
                            foreach (byte bb in (byte[])kv["data"])
                            {
                                temp2 += bb.ToString("X") + " ";
                                temp["data"] = temp2;
                            }
                        }
                        rtn.Add(temp);
                    }
                    else
                        hive(subkeyIndex, b, indent, false, search, pointer, rtn);
                }
            }
            //only has values when processing kv data. only relevant then
            return (rtn);
        }

        public static Dictionary<String, Object> get_kv(int index, byte[] b)
        {
            Dictionary<String, Object> kvKey = new Dictionary<String, Object>();

            int size = EndianByteLilToBig32(new byte[] { b[index], b[index + 1], b[index + 2], b[index + 3] });
            int signature = EndianByteBigToBig32(new byte[] { b[index + 4], b[index + 5] });
            int nameLength = EndianByteLilToBig32(new byte[] { b[index + 0x6], b[index + 0x7] });
            int dataLength = EndianByteLilToBig32(new byte[] { b[index + 0x08], b[index + 0x09], b[index + 0xA], b[index + 0xB] });
            int type = EndianByteLilToBig32(new byte[] { b[index + 0x10], b[index + 0x11], b[index + 0x12], b[index + 0x13] });
            String name = "";
            uint dataInt = 0;
            UInt64 dataInt64 = 0;
            byte[] data;
            //vk data can be the data itself, or it can be a pointer to a data block
            //the greatest bit (after converting to big endian) will be 1 if data is 
            //in the vk itself (resident), or 0 otherwise in binary form
            bool isResident = false;
            if (dataLength >= 0)
            {
                isResident = true;
            }

            for (int i = 0; i < nameLength; i++)
                name = name + (char)b[index + 0x18 + i];
            kvKey["name"] = name.Equals("") ? "(default)" : name;

            //data is directly in kv
            if (!isResident)
            {
                //when resident(data inside the key), this is signified by highest bit being 1
                //the actual datalength is the least greatest bits. e.g.,:
                //10000000000000000000000000000101 for data length of 5. so we remove the greatest
                //bit by substracting 10000000000000000000000000000000 (0x80000000).
                uint l = ((uint)dataLength) - 0x80000000;
                data = new byte[l];
                for (int i = 0; i < l; i++)
                    data[i] = b[index + 0x0C + i];
            }
            else
            {
                index = EndianByteLilToBig32(new byte[] { b[index + 0xC], b[index + 0xD], b[index + 0xE], b[index + 0xF] });
                index += 0x1000;
                //size is negative if in use. make positive for real value
                /*size = EndianByteLilToBig32(new byte[] { b[index], b[index + 1], b[index + 2], b[index + 3] });
                size = (~size) + 1;*/
                size = dataLength + 4; //changed code..not using size above, because using size in data block includes padding
                data = new byte[size + 1 - 4];
                for (int i = 0; i < size - 4; i++) //[size [data].... so minus space of size=4. 
                    data[i] = b[index + 4 + i]; //4 for size
            }
            switch (type)
            {
                case (T_QWORD):
                    kvKey["type"] = "QWord";
                    dataInt64 = EndianByteBigToBigU64(data);
                    kvKey["data"] = dataInt64;
                    break;
                case (T_DWORD_BIGE):
                    kvKey["type"] = "DWord";
                    dataInt = EndianByteBigToBigU32(data);
                    kvKey["data"] = dataInt;
                    break;
                case (T_DWORD_SMALLE):
                    kvKey["type"] = "DWord";
                    dataInt = EndianByteLilToBigU32(data);
                    kvKey["data"] = dataInt;
                    break;
                case (T_STRING):
                case (T_STRING2):
                    String temp = "";
                    foreach (byte bb in data)
                        if ((char)bb >= 0x20 && (char)bb <= 0x7e)
                            temp += (char)bb;
                    kvKey["type"] = "String";
                    kvKey["data"] = temp;
                    break;
                case (T_BINARY):
                    kvKey["type"] = "Binary";
                    kvKey["data"] = data;
                    break;
            }
            return kvKey;
        }
        public static UInt64 EndianByteBigToBigU64(byte[] by)
        {
            int offset = (8 * by.Length) - 8;
            UInt64 result = 0;
            foreach (byte b in by)
            {
                UInt64 temp = (offset == 0) ? (UInt64)b : ((UInt64)b) << offset;
                result = result + temp;
                offset -= 8;
            }
            return result;
        }
        public static uint EndianByteLilToBigU32(byte[] by)
        {
            int offset = 0;
            uint bigE = 0;
            foreach (byte b in by)
            {
                bigE = (offset == 0) ? bigE + (uint)b : bigE + (((uint)b) << offset);
                offset += 8;
            }
            return bigE;
        }
        public static uint EndianByteBigToBigU32(byte[] by)
        {
            int offset = (8 * by.Length) - 8;
            uint result = 0;
            foreach (byte b in by)
            {
                uint temp = (offset == 0) ? (uint)b : ((uint)b) << offset;
                result = result + temp;
                offset -= 8;
            }
            return result;
        }
        public static int EndianByteLilToBig32(byte[] by)
        {
            int offset = 0;
            int bigE = 0;
            foreach (byte b in by)
            {
                bigE = (offset == 0) ? bigE + (int)b : bigE + (((int)b) << offset);
                offset += 8;
            }
            return bigE;
        }
        public static int EndianByteBigToBig32(byte[] by)
        {
            int offset = (8 * by.Length) - 8;
            int result = 0;
            foreach (byte b in by)
            {
                int temp = (offset == 0) ? (int)b : ((int)b) << offset;
                result = result + temp;
                offset -= 8;
            }
            return result;
        }
    }
}
