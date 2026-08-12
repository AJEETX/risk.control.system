using System.Text;

namespace risk.control.system.Seeds
{
    public static class SampleFiles
    {
        public static byte[] GetValidPolicyPdfBytes(string policyNumber)
        {
            // Sanitize policy number and escape PDF reserved string characters
            string safePolicyNumber = string.IsNullOrWhiteSpace(policyNumber)
                ? "N/A"
                : policyNumber.Replace("(", "\\(").Replace(")", "\\)");

            // Assemble the PDF text stream content using PDF text layout operators
            var streamText = new StringBuilder();
            streamText.AppendLine("BT");

            // Title
            streamText.AppendLine("/F2 16 Tf 20 TL 50 740 Td");
            streamText.AppendLine("(Policy Schedule / Policy Bond) Tj T*");
            streamText.AppendLine("8 TL T*");

            // Note / Callout Block (Italics, wrapped)
            streamText.AppendLine("/F3 8 Tf 11 TL");
            streamText.AppendLine("(Note: this is the issued policy document, not the proposal-stage underwriting) Tj T*");
            streamText.AppendLine("(file. The full underwriting file \\(proposal form + medical questionnaire\\) for) Tj T*");
            streamText.AppendLine("(this policy could not be located - this schedule is the only proposal-time) Tj T*");
            streamText.AppendLine("(record available. It carries the LA/nominee/sum-assured details that) Tj T*");
            streamText.AppendLine("(were finalized at issuance, but it does not carry the medical questionnaire) Tj T*");
            streamText.AppendLine("(answers - policy schedules never do. Any non-disclosure assessment against) Tj T*");
            streamText.AppendLine("(this document is necessarily a thinner comparison than one made against) Tj T*");
            streamText.AppendLine("(a full underwriting file.) Tj T*");
            streamText.AppendLine("14 TL T*");

            // Key Policy Details (Fixed double parenthesis typo here)
            streamText.AppendLine("/F2 10 Tf 14 TL");
            streamText.AppendLine($"(Policy Number: {safePolicyNumber}) Tj T*");
            streamText.AppendLine("(Policy Issue Date: 2019-08-10) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Life Assured / Proposer
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Life Assured / Proposer) Tj T*");
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine("(- Name: Geeta Devi Sharma) Tj T*");
            streamText.AppendLine("(- DOB: 1962-07-03) Tj T*");
            streamText.AppendLine("(- Gender: Female) Tj T*");
            streamText.AppendLine("(- Address: 47, Kotwali Road, Lucknow, Uttar Pradesh - 226001) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Nominee
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Nominee) Tj T*");
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine("(- Name: Pooja Sharma) Tj T*");
            streamText.AppendLine("(- Relationship to Life Assured: Daughter) Tj T*");
            streamText.AppendLine("(- DOB: 1985-11-20) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Plan Details
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Plan Details) Tj T*");
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine("(- Plan: Whole Life Cover) Tj T*");
            streamText.AppendLine("(- Sum Assured: Rs. 15,00,000) Tj T*");
            streamText.AppendLine("(- Annual Premium: Rs. 22,000) Tj T*");
            streamText.AppendLine("(- Premium Paying Term: 25 years) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Risk Classification
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Risk Classification) Tj T*");
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine("(Standard rates applied at issuance. No extra premium/loading recorded on this schedule.) Tj T*");

            streamText.AppendLine("ET");

            string textContent = streamText.ToString();
            int textBytesCount = Encoding.ASCII.GetByteCount(textContent);

            // Structure raw PDF/1.4 object catalog
            string header = "%PDF-1.4\n";
            string obj1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            string obj2 = "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n";
            string obj3 = "3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R /F2 5 0 R /F3 6 0 R >> >> /MediaBox [0 0 612 792] /Contents 7 0 R >>\nendobj\n";
            string obj4 = "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n";
            string obj5 = "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n";
            string obj6 = "6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>\nendobj\n";
            string obj7 = $"7 0 obj\n<< /Length {textBytesCount} >>\nstream\n{textContent}\nendstream\nendobj\n";

            var sb = new StringBuilder();
            sb.Append(header);

            int offset1 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj1);
            int offset2 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj2);
            int offset3 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj3);
            int offset4 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj4);
            int offset5 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj5);
            int offset6 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj6);
            int offset7 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj7);
            int startXref = Encoding.ASCII.GetByteCount(sb.ToString());

            // Write xref table with exact byte offsets
            sb.Append("xref\n0 8\n");
            sb.Append("0000000000 65535 f \n");
            sb.Append($"{offset1:D10} 00000 n \n");
            sb.Append($"{offset2:D10} 00000 n \n");
            sb.Append($"{offset3:D10} 00000 n \n");
            sb.Append($"{offset4:D10} 00000 n \n");
            sb.Append($"{offset5:D10} 00000 n \n");
            sb.Append($"{offset6:D10} 00000 n \n");
            sb.Append($"{offset7:D10} 00000 n \n");
            sb.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n");
            sb.Append("startxref\n");
            sb.Append($"{startXref}\n");
            sb.Append("%%EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }
        public static byte[] GetValidJpgBytes()
        {
            // Pre-encoded 100x100 JPEG of a male portrait avatar
            string base64Jpg =
                "/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBxIQEhUREBIVFRUWFhUVGBcXFx0VGBgVFxUYFxUWFRcYHSggGBolHRUXITEiJSkrLi4uGB8zO" +
                "DMtNygtLisBCgoKDg0OGhAQGy0lICUrLS0tLSsrLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLf/AABEIAMYA/wMBIgACEQEDEQH" +
                "/xAAcAAACAgMBAQAAAAAAAAAAAAAAAQYHAwQFAgj/xAA/EAABAwEFBQYDBQcEAwEAAAABAAIRAwQFEiExBkFRYXEHEyKBkaEyUrFCYsHh8BQj" +
                "cpKy0fEzU4KiJDTCFf/EABkBAQADAQEAAAAAAAAAAAAAAAABAgMEBf/EACYRAAICAgIBBAIDAQAAAAAAAAABAhEDIRIxQQQyUWETcYHw8UL/2gA" +
                "MAwEAAhEDEQA/ALgQhCkgE0BCAE0JoAQhCAaEIQAhCSAaFjrVmsaXPIa0ZkkwAOZOigO0faZTpk07Ezvn/N9gH294VZSUeyyi30WEmqKPaVbGE469IE5" +
                "wGh5HIAnCPKVjPaTbJyrOJOjS1nqQGCB1VPy/Rb8b+S+UKlbL2r2poGPu3nXNsE+bSBHOOkqRbOdpvfviuynTaTAOYHMk5wNN3orLIiHBlkIXOo39ZX5Nt" +
                "FInSA8EzwGea6IKuUGhCEAIQhACEIQAhCEAIQmEAk0IQGBNJNQAQhCkDQhNACEBNACEIQCJhcu/L7pWRmOqc9zR8R6cOq422e1rLFIEF7WggfedME9B64" +
                "gqV2u2lqPze/E9wk8uDQNwE6fms5z8LsvGN7Z2Nttu3VycZOEfDTacuvM8z7KA2y9KtUZnCz5W5COfFaYBfLnGTn7R/dbhDe7EanF/1Coo1t9l7vSNRvhgN1O/gN" +
                "P7+y69208TNYYCS47zk2B9cvzXJrU/Hgb0XYZWDAGDRpyHFwiSemXmSrMhHupZziE5F2ZHBoEwPUfoStjBUBwMEaTAkjcGgbjnCzWWs1sPdm6BruzJH1LyOQXq" +
                "229rDAgCM+fGeZJjpi4rM0oQovaA4EnmD9Dw5/hmrF2W2ttFCmKeTmtbIDvsgnKN56SoXdN50hnVjLdznKefLkpHUttNop1SBLnkgcxoTyge6zcpJ6LqEWtloXBelau" +
                "AalMAcRGvScl3FCbivl9TCSIbl8I1PIfrVTOg/EJIhdOOVo5pxpntCaFoUBCEIAQhCAEIQgBCEIDAhCFAGhCFIGgIQgGmkhANYrTaG02ue84WtBJJ3AarKoh2qXmbPd9TCM" +
                "3ltMng12p9o81DdIFH7cbRm02qtWZOE1CGg/I0gDLnExxKjFOk+0O3k8f7of4vLF6An8vVT3Ym6RgDy3MrCc+Cs6MePm6I9Y9marhoc8+XNbQ2Kr7uas2jZNy6NksQl" +
                "crzzs7V6eBT1fZK0McCGmcs+PBeG7M2gFssPDTiZJ91e9Oxjgs9OyNGZAlWWWbKywwRStDZK01BOAieIjX8pW/S2ErO8TiAdeMRp/hXDCytaFdNszpLwUVfGx1agMp" +
                "cPi6xOv8AZcmneZD2h5MN8LRvk/E4/rcvoq0UGvaWuEgiFQO2l2/s1rewt8M4geRU/sq6fRZWw98CoQ3Fh3QM8+HNWfZxAVIbFUGkNOQdIjOFddgaQwSTpv3cslrhf" +
                "aMMy8m0hCFuYghCEAIQhACEIQAhCaA10JJoATSQgGmkmgGhJMIBquO26r/4lOnn4nl07vC06/zfVWOqv7eCRZ7O4f7rgf5PyVZ9Fo9lGU2nFh4mPUq7LgsQZRYIiGh" +
                "U82nFRjt2IBXtYafgb0C5M26O3BqzPZ6I4LfoUz5LWosPFb1Fqwo6LNiMknQk5kIdCtRRg0rYYFp0ittq1iZTPRCrPtQu/wDf0KwHxeA8DuAPrHmrPa1RLtBoy2i6JDXOdHR" +
                "hI9wpl0Vj3RHtk7pxPw0TkM4OeEjLDzbP15K4LKzCxoO4AKCdntMGq4jQMMdMUCT0VgBa4FqzDM90NCELcxBCEIAQhCAEIQgBNJNAayEIQAmkmgGmvITCAaYSCaA" +
                "agfbRZO8u1z4zp1KbweEnAf61PFCO1cGrZP2Vh8VWXDmaRDg3lJgSqzdIvCLlKkUDZh4gD84+sK761uZZ6TXPIGWQ4mNypa6bO59poscCCazQQdcsyD6FXHbbkbXqB" +
                "1QS0NAA4Deuaa2dWN6OU7b6iw+IE9OPDNdi5duLNXIGbTzyWna691WYYaraR64c+YxETvzCxWO8Lse4Op02DFo4Fp144HEjTeFWkkbK26J+x4c2RoViLMlr3dbmOENI" +
                "wrqWanjClUykm4nCvC86VmzqvDQeK0KW3ViMjvZI5H0GWq6G0t3WY+Ou0HCP1kNVAbFfF1trBgs4xGY8JqGAJJwsDt2aladIirVsn937UUKx8LsuK1Nuqg7ukdQXnzkaL3" +
                "dlnslQ+Cm1rumB06w4ZOaYzggLztpYD+z040ZVaf8AiQ4fUhTLcWUWpI9dm1ODW4RT/EH+lTkKI7E0O7a13+6XnPcGZAf1eil4WuL2mGZVIEIQtTIEIQgBCEIAQmhACEIQ" +
                "GqhCEA0IQgGmkmgGE0k0A1ENt7IX1rK6YDe9J9Gx7kKXKL9odJxs2Jk4g6BGuY/JZ5VcGdHpXWVFbtuYf/qse34Rjef4w0h3u4HzKmlvYXtwNMTkeij2zfw0ar5xmpUY/FrLgc" +
                "M/yt9VJ4MlcbejscUptIjto2NL7O6gKzcJf3kvZieHGQfECJkEjOcjlEBR607Lmz020RUc4NdiDgAHaQA1xzAEaCFYz6kBc6pQxu0/UqssrqkaY8Ubto52zdSpAaZgZSdTukqwrpdl5" +
                "KJvp4Mmak58lIbqeW5JidS2TninDRz9r7v78YXYsPIxn96NeijFl2NY+uy0Oe4PYRoBBgYYMDhkeIVjWpkhctlKCt2qlZzRlcKMzLA0v717i98RJgZAyAAAMpRf1AVLO9vIH0cCtikF" +
                "6qOAgHQmfJoLj9Ffsx8o83bTaRRwHJgII6t19fquyuJs5ZHU8eKIc4uaBuaT4QfJdtaYvbZT1FKdJ6X+ghCFoYAhCEAICE0AIQhACEIQGqhCEAJpJoBprymgPSEgmgGtK+rH31F7N" +
                "8Yh/E0yPot1NQ1aomLadoqS/and0zVYIPeUXO/41B+BIUoInMb81u7WbO0qtCu8BwPdvdhboXtaXNMRxAOS4Fx2/G0NOoA9IykrjnBx7PQjljN2jo90tK0EtOWXErpvqhonkohf" +
                "G1VGnImTpCwa2dUZ/JJ7PZmU4a4wXEkScyfPVSGwMDRJVE0NpX1LQ1zqbX4TDS4SQNYBPwhS609obQO5ZRD8hJccTY0IgrWNRMsj5rTLItVQFuJjgZ0gyCtKzVsWf6lRO4dr6" +
                "LmDG1tIEnIDCPLcFI7stNN5JY4HiP1qrOVsy40mdVqx45rBvCm4kfxOA/ArLigSVrXJFWtXqEThLKY8gXGP5gtVvRg5Vs7dAZLMvLV6W6OZuwQhCkgEIQgBCaEAIQhACEIQGqkh" +
                "CgDQkmpAwhKUwUA00pTQDTXlNAD2BwIOhBB6HIqkLVan2YvpElrmu7sEfddhP9J9VeCqTtVuzuK4tAnu62p3Cq0CW8sQE+TllljaNcUqZyto9oXmi1rXQSPFnoSIAKid02Vjjjq0q" +
                "zxucIgnf8RRbbSHEU5yBk884A/XAKW7OXjSw4QAcowmI9VzNcUdkGpPYXXTs1Mh7bO4u34hOuXGF3qNmstQ4jZoccj4eAE+S17RetCiJdSjz6Z8tV6ufa2g97QKUa55+vRLidnP" +
                "HVJM3bzuik6nAs1QwDGDCDOZ0c4Rn9VHtlb0NCv3ZxA4sJpu+Ju6d+/hxVkC3tImIy/UFQG9rGxlrbaWZxm6Pb3gzp0zUSq9HK5XeicXja9AHRlpv5rqbN0oo4iIxvc/ymG+zQoj+0" +
                "itUpsZ8bzg6D7TugEny9bAoUg1rWtyDQAOgEBdGJbs4sr1RlC9JBNbmIIQhACEIQAmkhANCEIAQhCA1EJIUAaEkKQNCSFJB6lel4TlQD0mvIKaEnpcDbm7WWmyOp1NMTTI1Bm" +
                "A4eq7y420V4UhTdSLgXuiGjOIIOfDRUm0osvBXJHzftHYKlmqCnVEED4oycBvby0y3ErobOXoLPJIl0DXdJy/wprthcgtlIDRzZIO/wCEw3oSQqsvCy1rM8srAjMQc4dh0DTx0WKqao" +
                "2dwlZYFa0MqjEQPFmeEkZLqXSWshrQIAz3ZQd/GfLRQSwX4zDhOWUD0kZ+vqtuntI0kyIkR7ZZc81jwZ0rKiwqt7h3hcThGXIjMTA009JXMr2ylSDxnvwxnjJJaAGj7RjKBJmOsDs98" +
                "1KtXDTY+oXCMLRJIBBExzM8M1Z3Z7sdUpEWu3D97n3dOcQpg/aO7HrppK0/HRjLJZ3thbifSBtNobhqPBDGf7VMmYP33QCeGnFTZhyWkxy1nVnMJLTv03K/NQMuDkdlC4tqvw" +
                "024m0H1SNWsLcXkHloPqte69trFXqCgKpp1ycPc1mupVMUTADhn1BIW0Zxl0zKUJR7RI0IQrFQQhCAEIQgBNJNACEIQGmhJCgAgpIUgcoleZTUkDlOVyb72is1jE2iq1p3NHieejR" +
                "mq52i7Wn5tslIM+/U8TvJgyHmT0UNpF445S6LZtFoZTaX1HNY0aucQAPMquNsu1anQinYcNR2+o4EsA+6MsR56dVUl8bRWi0uJr1X1DqMRyHQaDyC41aoXZnUKjZrHGl2XTct92" +
                "q00m2itXe51TxYWnBTYNzWtb01MlZqc4sRUK7O75GA2Z50Ms6HUeqnL152Xlz2duNR46MriuTeVlZUGGq0OadQuqDK17TRV0yJRI4zYiyvzaXtzzEzI4ceUrs2Hs8sTyC5r92WKIidI3" +
                "Zj0CyUiWlSq44IlaRk2zKUEkbFyXBZbID+z0WU5GZA10OZXULkmBY36rRmSRtscvD2yikV7hVlsstGqKSrvtVqsp1LMWgd6C90xngAjXqQp9fF5Ms9MvcdFQW099utdqdVOcDC0cGj" +
                "8yVWCXKjaN9kv2b20tNHEW1MTcR8LziadxAnNu/T3Vl3LtxZbQAHu7l/B/wz91+nrBVE0yGNDN8Ak896ziuWcRyXVZaeGEu+z6VaQRIzCaom49qa1nP7uqWj5T4mHq05DqIKsW49" +
                "u6VUAV24HfM3xN9NR7q1nJP00o9bJihY7PaGVG4qbg5vFpkeyyKTnBNJCAaEIQGihKUKCQQUlU/artZUbXFjoVHMa1s1cBglx0biGYAGscUbomMXJ0T2/wDauyWIE1qoxfI3xPPkNP" +
                "OFWW0XajXqy2h+4YeHiqebtB0Hqq6tVoJ9c/8AO9a9V+YVbbOiOOMfs3rZbn1HFznEknMkyTzJOZWk90jM5pE5pgKC5gqDejmsuBeQIUkUFjtBpPD2nMfRWvs1ewtFPXxBVLU4rr7L" +
                "3sbPVbJ8JMHoVhmx8lZaEqdFwsC9uZIRZyHsDm6ESvdM7iudI3s0abM137qMZBaP7NOa3LKMKtHRWW0SGk5eDmVipVslsUBvWtnPVGZgXm1VQxpJQ54bqojtrfwp0nNacyDnwV" +
                "Zy4otCDlKiA9om0pqvNNp8IMeahd3NxEvOm7oDl7/RYLVVNeoTPhGc8t5/XJbdEZe/kMgFpihxW+zou39I2adXFWDVt22r48M5N+q07pEVnOP2WOd6LHUqwDJzK1JvR03fDlqky2" +
                "OpvadAThyXh1WGieAWpeLobS4kz7oJa2S+x7TVrG6Q8jTMHUcHDQ+asG7+0Rg/9psNgHvGZ+E6FzD7xPRUvtFUyGe5q3xax3NNztDTLTzGisUnCM200fRd33jRtDcdGo14icjnB4jUL" +
                "aXz5sPaqlOkHsJDmPljpiGnUTvby0V+WC1CtTbUbo4A+e8eqlHHlw8En4ZsBCSakwOdKJSQoBoX9ejbJZ6ld32G5Di45NHmYXzVedtdVtBqPMuqYiTzklWr2zXrApWYHjUd7tZ/9eypu0" +
                "Oza75XR5FVfZ1Y48Y2eqrc1ifoDwWzXGhWAtyKF2j2WyJQ7KF6sxlsLzWOiFvFjqjevJCy1WyJWLcgZ5c1YYgwfJZwUnMlCrRNtjdqwwCjWMfK46dDwKntKsHQRvVDtcWmDmFIbh2mq" +
                "2YgNONvyPOn8J1HuFhPFfReMl5L3sNHE1YrVTwKP7P9o1he0Cq51F2/G2Wzye2R6wtq9tprK8TTtFJ3R7T+Kq40gr5G9QtRJhd+hWAaqxbtfZqZ8VZuXA4j6Bat6dqbGjDZqTnu0xP8LfT" +
                "U+ypFS+C04onV/wB9Nptc5zg1o1JMKlNqNpHWx5p0pFOdTkXczwbyXPve87RbX47Q8nOQ0ZNHRv4leaVEBojefbmtYYafKQ5WqjpCZQgBg0kSd5PGOC6dnpiY4LHZKYx5dVlqOgu" +
                "P69VuXWgu2MVQnQtLVxalQzHNdiyGGPJGo9lxK3xIUn0jp1qsuawckXoZrsZ8uEf3WK6hiqgnQZ+QXizOx2mfvSoDdr+Tc2jqeJ2/RY7TWLqVCk3UiPUrBflWXmOK9WatgIq691TBb/Gc" +
                "mf8AYg+Skq3tk4srhRDbMw/6cGoR850YDyGZVl7AXpM0HH7zZ9x6Z+qqK6aRpUWl5JfUOOTz1LuqlGzFsNKqx4JyIPvnKujacecKLpQkx0gEaET6poeSc1CSEBQXaTbDVt1cn7LgwdGtH4" +
                "z6qFV25uHKfMZoQszuftRsB8sWu05FCFIZmsA1CxWh2aaFBP8AyZmGWrAQmhAwcMp4p0wkhSR5PFVi9WYB3hPkUIQeTLUpEGMXrmsD+YCaEJaMTRyCz0mTpl0yQhCqRtUqe5bYb7" +
                "BCENYmzY2bzzHpmsFpdqmhC76GMqeekLh2g5oQoM8nSN25jHeO3hhXi4M3uceBQhSQu4mC3vlx6reu+z96aNKf9R5cTyZkB7n2TQiIW5f35JF3vePJGQacLQdwGUruXU85Ru4oQro6" +
                "ol0XFVxWekT8oHpl+C3kIQ8mfuf7P//Z";

            // Sanitize any accidental line breaks or spaces before decoding
            string sanitizedBase64 = base64Jpg.Replace("\r", "").Replace("\n", "").Trim();
            return Convert.FromBase64String(sanitizedBase64);
        }

        public static byte[] GetValidClaimPdfBytes(string policyNumber)
        {
            // Sanitize policy number and escape PDF reserved string characters
            string safePolicyNumber = string.IsNullOrWhiteSpace(policyNumber)
                ? "C123456"
                : policyNumber.Replace("(", "\\(").Replace(")", "\\)");

            // Assemble PDF text stream content using standard PDF operators
            var streamText = new StringBuilder();
            streamText.AppendLine("BT");

            // Title
            streamText.AppendLine("/F2 16 Tf 20 TL 50 740 Td");
            streamText.AppendLine("(Claim Form) Tj T*");
            streamText.AppendLine("10 TL T*");

            // General Claim & Life Assured Information
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine($"(Policy Number: {safePolicyNumber}) Tj T*"); // Fixed double parenthesis
            streamText.AppendLine("(Life Assured Name: John Doe) Tj T*");
            streamText.AppendLine("(Date of Claim Intimation: 2023-10-10) Tj T*");
            streamText.AppendLine("(Life Assured Date Of Birth: 1995-01-01) Tj T*");
            streamText.AppendLine("(Address: 67 Mahoneys Road, Forest Hill, Victoria - 3131) Tj T*");
            streamText.AppendLine("(Date of Death: 2023-10-16) Tj T*");
            streamText.AppendLine("(Cause of Death \\(as reported by beneficiary\\): Kidney failure) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Beneficiary Section
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Beneficiary) Tj T*");
            streamText.AppendLine("/F1 10 Tf 14 TL");
            streamText.AppendLine("(- Name: Jack Smith) Tj T*");
            streamText.AppendLine("(- Relationship to Life Assured: Daughter) Tj T*");
            streamText.AppendLine("(- Address: 75 heywood street, Ringwood, Victoria - 3134) Tj T*");
            streamText.AppendLine("(- Payout Bank Account: A/C ending 8814, Australia National Bank,) Tj T*");
            streamText.AppendLine("(  Ringwood Branch, Victoria 3134) Tj T*");
            streamText.AppendLine("10 TL T*");

            // Beneficiary Statement Section
            streamText.AppendLine("/F2 11 Tf 16 TL");
            streamText.AppendLine("(Beneficiary Statement) Tj T*");
            streamText.AppendLine("/F3 9 Tf 13 TL");
            streamText.AppendLine("(\"My mother had been on dialysis for the last few years and her health) Tj T*");
            streamText.AppendLine("(kept declining. She passed away on 16th October 2023 at the civil) Tj T*");
            streamText.AppendLine("(hospital.\") Tj T*");

            streamText.AppendLine("ET");

            string textContent = streamText.ToString();
            int textBytesCount = Encoding.ASCII.GetByteCount(textContent);

            // Structure raw PDF/1.4 object catalog
            string header = "%PDF-1.4\n";
            string obj1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            string obj2 = "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n";
            string obj3 = "3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R /F2 5 0 R /F3 6 0 R >> >> /MediaBox [0 0 612 792] /Contents 7 0 R >>\nendobj\n";
            string obj4 = "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n";
            string obj5 = "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n";
            string obj6 = "6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>\nendobj\n";
            string obj7 = $"7 0 obj\n<< /Length {textBytesCount} >>\nstream\n{textContent}\nendstream\nendobj\n";

            var sb = new StringBuilder();
            sb.Append(header);

            int offset1 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj1);
            int offset2 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj2);
            int offset3 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj3);
            int offset4 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj4);
            int offset5 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj5);
            int offset6 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj6);
            int offset7 = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.Append(obj7);
            int startXref = Encoding.ASCII.GetByteCount(sb.ToString());

            // Write xref table with exact byte offsets
            sb.Append("xref\n0 8\n");
            sb.Append("0000000000 65535 f \n");
            sb.Append($"{offset1:D10} 00000 n \n");
            sb.Append($"{offset2:D10} 00000 n \n");
            sb.Append($"{offset3:D10} 00000 n \n");
            sb.Append($"{offset4:D10} 00000 n \n");
            sb.Append($"{offset5:D10} 00000 n \n");
            sb.Append($"{offset6:D10} 00000 n \n");
            sb.Append($"{offset7:D10} 00000 n \n");
            sb.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n");
            sb.Append("startxref\n");
            sb.Append($"{startXref}\n");
            sb.Append("%%EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
