using NBitcoin;
using NBitcoin.BitcoinCore;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.Stealth;
using Stratis.Bitcoin.Controllers.Models;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.P2P.Protocol;
using Stratis.Bitcoin.P2P.Protocol.Payloads;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GChain
{
    /*
     * 
key: VCDdhQmCv52LruvzVYx6qA2PhpPHc6AbyE7fyABR2YyZAruqk6CA
secret: L2GYFQRS4YDpSq7NCrUjUNACGsvqr9MXsqVbGzhYatFRiLYq8waN
pub: 03ea92059103e5aebf931c11a5375e82a758cbebf6985c433dc6ed11f68ee4b5d8
addr: RyMBTkqrotubSuY8B3HtSBnkeUUA4PxP3f
     *  
     * 
     * */

    /*
    CTransaction(hash=30794c7c4649dc3b75de7979794b98f8438180d68f5a3f7010c4fc7ed99b0ec9, ver=1, vin.size=3, vout.size=2, nLockTime=0)
        CTxIn(COutPoint(1fa25c838cbc11ae7a10defe698b38aaa7bb82d210e9054466b9e1ee041ecc54, 0), scriptSig=304602210091c384590a67fc)
        CTxIn(COutPoint(00229d1996c8a9752d899ce327529c199ea9e80f5f77005b527c05043235885b, 0), scriptSig=3044022073c5b9f25efbec0e)
        CTxIn(COutPoint(7d0518cac027f5351fd39104e906a9d3e2197120c7e33128b2244ef24baca1fc, 0), scriptSig=3044022058d6b5be9c69b514)
        CTxOut(nValue=18.00000000, scriptPubKey=OP_DUP OP_HASH160 ddba4d199aa1)
        CTxOut(nValue=120.00000000, scriptPubKey=OP_DUP OP_HASH160 ee05e2439781)
    */

    public class ChainClient
    {
        public Network net;
        public string dataDir;

        public ChainClient(Network net, string dataDir)
        {
            this.net = net;
            this.dataDir = dataDir;
        }


        private Dictionary<OutPoint, Money> printUnspents(BitcoinAddress addressToWatch)
        {
            Console.WriteLine("addr : " + addressToWatch);
            List<OutPoint> outs = new List<OutPoint>();
            Dictionary<OutPoint, Money> dic = new Dictionary<OutPoint, Money>();
            // RPCClient client = new RPCClient(new NetworkCredential("usuariox", "SupEr3421Senha_SECRETAandGRandE"), "54.152.50.251", net);
            RPCClient client = new RPCClient("usuaraw:SupEr3421aw", new Uri("127.0.0.1"), net);

            int lastBlock = client.GetBlockCount();
            for (int i = 1; i < lastBlock; i++)
            {

                Block b = client.GetBlock(client.GetBlockHash(i));

                if (b != null)
                    foreach (Transaction t in b.Transactions)
                    {

                        /*                        Transaction fundingTransaction = client.GetRawTransaction(t.GetHash(), true);
                                                Console.WriteLine(fundingTransaction);
                                                Console.WriteLine("----------------------------");
                        */
                        int n = 0;
                        foreach (TxOut vout in t.Outputs)
                        {
                            BitcoinAddress addr = (vout.ScriptPubKey.GetScriptAddress(net));
                            BitcoinAddress destAddr = vout.ScriptPubKey.GetDestinationAddress(net);
                            // if (vout.ScriptPubKey.GetDestinationPublicKeys() != null && vout.ScriptPubKey.GetDestinationPublicKeys().Length == 1)
                            if (destAddr != null)
                            {
                                if (destAddr == addressToWatch)
                                {
                                    // if (address.ToWif() == addressToWatch.ToWif())
                                    Console.WriteLine("++ received ++");
                                    Console.WriteLine(t.GetHash().ToString() + " - " + n);
                                    Console.WriteLine(vout.Value.ToString());
                                    outs.Add(new OutPoint(t.GetHash(), n));
                                    dic.Add(new OutPoint(t.GetHash(), n++), vout.Value);
                                }
                            }
                        }

                        foreach (TxIn vin in t.Inputs) //.AsIndexedInputs())
                        {
                            {
                                //if(t.IsCoinBase)
                                {

                                    if (dic.Remove(new OutPoint(vin.PrevOut.Hash, vin.PrevOut.N)))
                                    {
                                        Console.WriteLine("-- spent -- ");
                                        Console.WriteLine(vin.PrevOut.Hash + " - " + vin.PrevOut.N);
                                    }
                                }
                            }
                        }
                    }
            }

            Console.WriteLine(".");
            // client.GetTransactions(client.GetBestBlockHash()).First().GetHash;
            return dic;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Payment(RPCClient client, Network net, BitcoinSecret sender, BitcoinAddress receiver, Money amount)
        {
            Money fees = Money.Parse("0.001");
            var tx = new Transaction();
            tx.Version = 1;
            Money totalSent = Money.Zero;

            Dictionary<OutPoint, Money> availableMoney = listIdexedUnspent(client, sender.GetAddress());
            foreach (var unspent in availableMoney)
            {
                TxIn vin = new TxIn(unspent.Key);
                vin.ScriptSig = sender.GetAddress().ScriptPubKey;

                tx.Inputs.Add(vin);
                totalSent = totalSent + unspent.Value;
                if (totalSent > amount + fees)
                    break;
            }

            if (totalSent < amount + fees)
                throw new Exception("Not enough funds");

            Console.WriteLine("availableMoney Money : " + totalSent.ToString());
            tx.Outputs.Add(new TxOut(amount, receiver)); //.GetAddress()));
            tx.Outputs.Add(new TxOut(totalSent - amount - (fees), sender.GetAddress()));
            tx.Sign(net, sender.PrivateKey, false);


            Console.WriteLine(tx);
            client.SendRawTransaction(tx);

            /*
            using (var node = Node.Connect(net, "127.0.0.1", ProtocolVersion.PROTOCOL_VERSION, true, default(CancellationToken))) //Connect to the node
            {
                node.VersionHandshake(); //Say hello
                //Advertize your transaction (send just the hash)
                node.SendMessage(new InvPayload(InventoryType.MSG_TX, tx.GetHash()));
                //Send it
                node.SendMessage(new TxPayload(tx));
                Thread.Sleep(500); //Wait a bit

                for (int i = 0; i < 6; i++)
                    client.setGenerate(true);
                client.setGenerate(false);

                Thread.Sleep(500); //Wait a bit
                //var chain = new PersistantChain(net, new StreamObjectStream<ChainChange>(File.Open("MainChain.dat", FileMode.OpenOrCreate)));
               // node.SynchronizeChain(chain);
            }
            for (int i = 0; i < 6; i++)
                client.setGenerate(true);
            client.setGenerate(false);
                         * */

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Dictionary<OutPoint, Money> listUnspent(RPCClient client, BitcoinAddress addressToWatch)
        {
            Console.WriteLine("addr : " + addressToWatch);
            List<OutPoint> outs = new List<OutPoint>();
            Dictionary<OutPoint, Money> dic = new Dictionary<OutPoint, Money>();
            int lastBlock = client.GetBlockCount();
            for (int i = 0; i < lastBlock; i++)
            {
                Block b = client.GetBlock(client.GetBlockHash(i));
                if (b != null)
                    foreach (Transaction transaction in b.Transactions)
                    {
                        int n = 0;
                        foreach (TxOut vout in transaction.Outputs)
                        {
                            // BitcoinAddress addr = (vout.ScriptPubKey.GetScriptAddress(net));
                            BitcoinAddress destAddr = vout.ScriptPubKey.GetDestinationAddress(net);
                            // if (vout.ScriptPubKey.GetDestinationPublicKeys() != null && vout.ScriptPubKey.GetDestinationPublicKeys().Length == 1)
                            if (destAddr != null)
                            {
                                if (destAddr == addressToWatch)
                                {
                                    // if (address.ToWif() == addressToWatch.ToWif())
                                    Console.WriteLine("++ received ++");
                                    Console.WriteLine(transaction.GetHash().ToString() + " - " + n);
                                    Console.WriteLine(vout.Value.ToString());
                                    dic.Add(new OutPoint(transaction.GetHash(), n), vout.Value);
                                }
                            }
                            n++;
                        }

                        foreach (TxIn vin in transaction.Inputs) //.AsIndexedInputs())
                        {
                            {
                                //if(t.IsCoinBase)
                                {

                                    if (dic.Remove(new OutPoint(vin.PrevOut.Hash, vin.PrevOut.N)))
                                    {
                                        Console.WriteLine("-- spent -- ");
                                        Console.WriteLine(vin.PrevOut.Hash + " - " + vin.PrevOut.N);
                                    }
                                }
                            }
                        }
                    }
            }

            Console.WriteLine(".");
            // client.GetTransactions(client.GetBestBlockHash()).First().GetHash;
            return dic;
        }

        private void pay(BitcoinSecret sender, BitcoinAddress receiver, uint256 txid, bool pay)
        {
            RPCClient client = new RPCClient("usuaraw:SupEr3421aw", new Uri("127.0.0.1"), net);
            Transaction fundingTransaction = client.GetRawTransaction(txid);
            Console.WriteLine(fundingTransaction);
            Console.WriteLine("----------------------------");


            Transaction payment = new Transaction();
            payment.Inputs.Add(new TxIn()
            {
                PrevOut = new OutPoint(fundingTransaction.GetHash(), 0)
            });

            payment.Outputs.Add(new TxOut()
            {
                Value = Money.Coins(5.12m),
                ScriptPubKey = receiver.ScriptPubKey
            });
            payment.Outputs.Add(new TxOut()
            {
                Value = Money.Coins(4.2m),
                ScriptPubKey = sender.GetAddress().ScriptPubKey
            });
            /*
                        //Feedback !
                        var message = "Thanks ! :)";
                        var bytes = Encoding.UTF8.GetBytes(message);
                        payment.Outputs.Add(new TxOut()
                        {
                            Value = Money.Zero,
                            ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
                        });
             */
            Console.WriteLine(payment);
            payment.Inputs[0].ScriptSig = sender.GetAddress().ScriptPubKey;
            //also OK:
            //payment.Inputs[0].ScriptSig =
            //fundingTransaction.Outputs[1].ScriptPubKey;
            payment.Sign(sender, false);
            Console.WriteLine("------------------");

            Console.WriteLine(payment);
            // new RPCClient(new NetworkCredential("usuaraw", "SupEr3421aw"), "127.0.0.1", net).SendRawTransaction(payment);
            // using (var node = Node.Connect(net, "54.152.50.251", ProtocolVersion.PROTOCOL_VERSION, true, default(CancellationToken))) //Connect to the node

            /*
            if (pay)
                using (var node = TransactionModel.Connect(net, "127.0.0.1", ProtocolVersion.PROTOCOL_VERSION, true, default(CancellationToken))) //Connect to the node
                {
                    node.VersionHandshake(); //Say hello
                    //Advertize your transaction (send just the hash)
                    node.SendMessage(new Stratis.Bitcoin.P2P.Protocol.Payloads.InvPayload(payment));
                    //Send it
                    node.SendMessage(new TxPayload(payment));
                    Thread.Sleep(500); //Wait a bit
                }
            */
        }

        public void ListSecrets(NetworkCredential credentials, string host, Network net)
        {

            RPCClient client = new RPCClient("usuaraw:SupEr3421aw", new Uri("127.0.0.1"), net);
            IEnumerable<BitcoinSecret> secrets = client.ListSecrets();
            foreach (BitcoinSecret secret in secrets)
            {
                PubKey pub = secret.PubKey;
                Console.WriteLine("key: " + secret.PrivateKey.GetWif(net));
                Console.WriteLine("secret: " + secret);
                Console.WriteLine("pub: " + pub.ToHex());
                Console.WriteLine("addr: " + pub.GetAddress(net));
            }
        }

        public void ListAccounts(RPCClient client)
        {

            IEnumerable<RPCAccount> accounts = client.ListAccounts();
            foreach (RPCAccount account in accounts)
            {
                Console.WriteLine("name: " + account.AccountName);
                Console.WriteLine("amount: " + account.Amount.Satoshi);
                Console.WriteLine("amount: " + account.Amount.ToString());
            }
        }



        public void sendToAddress(RPCClient client, BitcoinSecret receiver, Decimal amount)
        {
            object[] par = new object[2];
            par[0] = receiver.PubKey.GetAddress(net);
            par[1] = amount;
            client.SendCommand("sendtoaddress", par);
        }

        public void CanHandshake()
        {

        }

        public BitcoinSecret keyGen()
        {
            return new Key().GetWif(net);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public BitcoinSecret keyGen(string seed, string password)
        {
            SHA1Managed sha1 = new SHA1Managed();
            var words = seed.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = words.Count(); i < 12; i++)
            {
                seed += " " + i * i;
            }
            Mnemonic mnemo = new Mnemonic(seed, Wordlist.English);
            ExtKey hdRoot = mnemo.DeriveExtKey(password);
            return hdRoot.PrivateKey.GetWif(net);
        }

        public BitcoinAddress addrGen(BitcoinSecret secret)
        {
            return secret.PubKey.GetAddress(net);
        }

        public void CanBuildStealthTransaction()
        {
            var stealthKeys = Enumerable.Range(0, 3).Select(_ => new Key()).ToArray();
            var scanKey = new Key();

            var darkSatoshi = new BitcoinStealthAddress(scanKey.PubKey, stealthKeys.Select(k => k.PubKey).ToArray(), 2, new BitField(3, 5), NBitcoin.Networks.NetworkRegistration.GetNetworks().FirstOrDefault());

            Console.WriteLine("darkSatoshi " + darkSatoshi);

            var bob = new Key();
            Console.WriteLine("bob wif" + bob.GetWif(NBitcoin.Networks.NetworkRegistration.GetNetworks().FirstOrDefault()));
            var coins = new Coin[] {
                new Coin()
                {
                    Outpoint = RandOutpoint(),
                    TxOut = new TxOut("1.00",bob.PubKey.Hash)
                } };

            //Bob sends money to satoshi
            TransactionBuilder builder = new TransactionBuilder(NBitcoin.Networks.NetworkRegistration.GetNetworks().FirstOrDefault());
            var tx =
                builder
                .AddCoins(coins)
                .AddKeys(bob)
                .Send(darkSatoshi, "1.00")
                .BuildTransaction(true);
            Console.WriteLine(tx);
            //		Assert.True(builder.Verify(tx));

            //Satoshi scans a StealthCoin in the transaction with his scan key
            var stealthCoin = StealthCoin.Find(tx, darkSatoshi, scanKey);
            //		Assert.NotNull(stealthCoin);

            //Satoshi sends back the money to Bob
            builder = new TransactionBuilder(NBitcoin.Networks.NetworkRegistration.GetNetworks().FirstOrDefault());
            tx =
                builder
                    .AddCoins(stealthCoin)
                    .AddKeys(stealthKeys)
                    .AddKeys(scanKey)
                    .Send(bob.PubKey.Hash, "1.00")
                    .BuildTransaction(true);

            //	Assert.True(builder.Verify(tx)); //Signed !
        }


        private OutPoint RandOutpoint()
        {
            return new OutPoint(Rand(), 0);
        }

        private uint256 Rand()
        {
            return new uint256(RandomUtils.GetBytes(32));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]

        public Dictionary<OutPoint, Money> listIdexedUnspent(RPCClient client, BitcoinAddress addressToWatch)
        {
            return null;
        }
    }
}
